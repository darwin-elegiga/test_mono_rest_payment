using MediatR;
using Microsoft.Extensions.Logging;
using VPay.Gateway.Common.Documents.Models;

namespace VPay.Gateway.Common.Documents;


public class GetDocumentPaymentDetailByTransactionIDQuery : IRequest<TransactionPayment?>
{
    public long TransactionId { get; set; }

    public GetDocumentPaymentDetailByTransactionIDQuery(long transactionId)
    {
        TransactionId = transactionId;
    }
}

public partial class GetPaymentByTransactionIdQueryHandler : IRequestHandler<GetDocumentPaymentDetailByTransactionIDQuery, TransactionPayment?>
{

    private readonly ILogger<GetPaymentByTransactionIdQueryHandler> _logger;
    private readonly IDocumentsApi _documentApi;

    public GetPaymentByTransactionIdQueryHandler(
        ILogger<GetPaymentByTransactionIdQueryHandler> logger, IDocumentsApi documentApi)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _documentApi = documentApi ?? throw new ArgumentNullException(nameof(documentApi));
    }
    public async Task<TransactionPayment?> Handle(GetDocumentPaymentDetailByTransactionIDQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        const string Action = "Find Transaction Payment";
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Action"] = Action,
            ["TransactionId"] = request.TransactionId,
        });
        try
        {
            var paymentDetail = await _documentApi.DocumentPaymentByTransactionID(request.TransactionId, cancellationToken).ConfigureAwait(false);
            return paymentDetail ?? null;
        }
        catch (Refit.ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogInformation("DocumentPaymentByTransactionID returned 404 for TransactionId: {TransactionId}", request.TransactionId);
            return null; // Handle 404 by returning null
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while fetching DocumentPaymentByTransactionID for TransactionId: {TransactionId}", request.TransactionId);
            throw; // Re-throw other exceptions
        }
    }
}
