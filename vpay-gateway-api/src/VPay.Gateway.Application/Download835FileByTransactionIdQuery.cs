using System.Net.Sockets;
using MediatR;
using Microsoft.Extensions.Logging;
using Refit;
using VPay.Extensions.Exceptions;
using VPay.Gateway.Common.Documents;
using VPay.Gateway.Common.Documents.Models;
using VPay.Gateway.Common.Kubera;
using VPay.Gateway.Common.Kubera.Models;

namespace VPay.Gateway.Application;

public record Download835FileByTransactionIdQuery(long TransactionId) : IRequest<Document?>;

public class Download835FileByTransactionIdQueryHandler : IRequestHandler<Download835FileByTransactionIdQuery, Document?>
{
    private readonly IMediator _mediator;
    private readonly IDocumentsApi _documentsApi;
    private readonly IUserInfo _userInfo;
    private readonly ILogger<Download835FileByTransactionIdQueryHandler> _logger;

    public Download835FileByTransactionIdQueryHandler(
        IMediator mediator,
        IDocumentsApi documentsApi,
        IUserInfo userInfo,
        ILogger<Download835FileByTransactionIdQueryHandler> logger)
    {
        _mediator = mediator;
        _documentsApi = documentsApi;
        _userInfo = userInfo;
        _logger = logger;
    }

    public async Task<Document?> Handle(Download835FileByTransactionIdQuery request, CancellationToken cancellationToken)
    {
        Transaction? trans = null;

        // First try to find the transaction in the Doc Sys Generation Storage API if the user does not have restricted payment types
        // because it is faster to get the payment information which is required for authorization and error messages.
        if (!_userInfo.IsInRoleWithRestrictedPaymentTypes())
        {
            TransactionPayment? payment = null;
            try
            {
                _logger.LogInformation("Download 835 - Attempting to get transaction ID {TransactionId} from Doc Sys Generation Storage API.", request.TransactionId);
                payment = await _mediator
                    .Send(new GetDocumentPaymentDetailByTransactionIDQuery(request.TransactionId), cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Allow the OperationCanceledException to propagate up so that the request can be cancelled in a timely manner
                // instead of being caught by the general catch block which will swallow it and continue execution to the
                // Kubera/Bartok API call.
                throw;
            }
            catch
            {
                _logger.LogWarning("Download 835 - Failed to get transaction ID {TransactionId} from Doc Sys Generation Storage API. Falling back to Kubera/Bartok API.", request.TransactionId);
                // Ignore any exception from this call and fallback to Kubera/Bartok API to get the transaction information
                // because some older transactions may not be in the Doc Sys Generation Storage API.
            }

            if (payment != null)
            {
                trans = new Transaction
                {
                    TransactionId = request.TransactionId,
                    ClientCode = payment.ClientCode,
                    BillingEntityCode = payment.BillingEntityCode,
                    PaymentTypeCode = string.Empty, // Not used in CanAccess method because role does not have restricted payment types.
                    DateLoaded = DateTime.UtcNow.AddDays(-3), // Only used for error message in case of exception in order to show 835 not found.
                };

                if (!_userInfo.CanAccess(trans.ClientCode, trans.BillingEntityCode, trans.PaymentTypeCode))
                {
                    throw new NotAuthorizedException($"Access is denied for the transaction with ID '{request.TransactionId}'");
                }
            }
        }

        // If transaction was not found then search in the Kubera/Bartok API as a fallback.
        // Older transactions that were loaded before the implementation of the underlying iSeries table
        // may not be in the Doc Sys Generation Storage API but should be in Kubera/Bartok API.
        if (trans == null)
        {
            try
            {
                _logger.LogInformation("Download 835 - Attempting to get transaction ID {TransactionId} from Kubera/Bartok API.", request.TransactionId);
                // Get the transaction information from Kubera/Bartok API
                trans = await _mediator.Send(new GetTransactionByIdQuery(request.TransactionId), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is SocketException || ex is TaskCanceledException || ex is IOException)
            {
                // Gracefully handle timeouts and other IO exceptions that otherwise would throw an Unhandled Exception.
                throw new TimeoutException($"Kubera access timed out for the transaction with ID '{request.TransactionId}'");
            }
            catch
            {
                throw;
            }

            if (trans == null)
            {
                _logger.LogWarning("Download 835 - Transaction ID {TransactionId} not found in Kubera/Bartok API.", request.TransactionId);
                // Unable to find transaction in Doc Sys Generation Storage API or in Kubera/Bartok API.
                throw new ItemNotFoundException(Edi835Statuses.TransactionMissing);
            }
            else
            {
                if (!_userInfo.CanAccess(trans.ClientCode, trans.BillingEntityCode, trans.PaymentTypeCode))
                {
                    throw new NotAuthorizedException($"Access is denied for the transaction with ID '{request.TransactionId}'");
                }
            }
        }

        try
        {
            _logger.LogInformation("Download 835 - Attempting to download 835 file for transaction ID {TransactionId} from Documents API.", request.TransactionId);
            var result = await _documentsApi.Download835(request.TransactionId, cancellationToken).ConfigureAwait(false);

            var fileName = result.Headers.ContentDisposition?.FileName ?? $"edi-{request.TransactionId}.835";
            var mediaType = result.Headers.ContentType?.MediaType ?? "text/plain";

            var stream = await result.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

            return new Document(
                stream,
                mediaType,
                fileName);
        }
        catch (ApiException apiException)
        {
            _logger.LogError(apiException, "Download 835 - Failed to download 835 file for transaction ID {TransactionId} from Documents API.", request.TransactionId);
            if (apiException.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                throw new ItemNotFoundException(Edi835Statuses.TransactionMissing);
            }
            else if (apiException.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                var detailVal = await apiException.GetContentAsAsync<ProblemDetails>().ConfigureAwait(false);
                if (detailVal == null)
                {
                    throw;
                }

                throw detailVal.Detail switch
                {
                    var s when s == Edi835Statuses.TransactionMissing => new ItemNotFoundException(Edi835Statuses.TransactionMissing),
                    var s when s == Edi835Statuses.NotNeededFromDocSys => new ItemNotFoundException(Edi835Statuses.NotNeeded),
                    _ => new ItemNotFoundException(Edi835Statuses.GetNotReadyString(trans.DateLoaded)),
                };
            }
            else
            {
                throw;
            }
        }
    }
}
