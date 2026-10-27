using System.ComponentModel.DataAnnotations;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Polly;
using Polly.Registry;
using Refit;
using StackExchange.Redis;
using Vpay.Gateway.Common.Vpay.Models;
using VPay.Extensions.Exceptions;
using VPay.Gateway.Api.Models;
using VPay.Gateway.Application;
using VPay.Gateway.Common;
using VPay.Gateway.Common.Documents;
using VPay.Gateway.Common.Documents.Models;
using VPay.Gateway.Common.Models;
using VPay.Gateway.Common.PlatformConfig;
using VPay.Gateway.Data.Sql.Transactions;
using static Microsoft.AspNetCore.Http.StatusCodes;
using AuthorizeAttribute = Microsoft.AspNetCore.Authorization.AuthorizeAttribute;

namespace VPay.Gateway.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize(Roles = "ViewAllDocuments")]
public class DocumentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly ILogger<DocumentsController> _logger;
    private readonly IAsyncPolicy<long> _cachePolicy;
    private readonly IUserInfo _userInfo;

    public DocumentsController(
        IMediator mediator,
        IMapper mapper,
        IReadOnlyPolicyRegistry<string> policyRegistry,
        ILogger<DocumentsController> logger,
        IUserInfo userInfo)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _userInfo = userInfo ?? throw new ArgumentNullException(nameof(userInfo));
        if (policyRegistry is null)
        {
            throw new ArgumentNullException(nameof(policyRegistry));
        }

        _cachePolicy = policyRegistry.Get<IAsyncPolicy<long>>(CachingPolicies.TransactionIdCachePolicy);
    }

    /// <summary>
    /// Gets the documents associated with the provider tin and payment id.
    /// </summary>
    /// <param name="providerTin">The tax id of the transaction to retrieve documents for.</param>
    /// <param name="paymentId">The client's id of the transaction to retrieve documents for.</param>
    /// <param name="token">A token that can be used to cancel the work.</param>
    /// <response code="200">If documents are retrieved successfully for the transaction.</response>
    /// <response code="404">If cannot find documents for the transactionId.</response>
    [HttpGet("{providerTin}/{paymentId}")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status404NotFound)]
    public async Task<ActionResult<List<DocumentDto>>> GetDocumentsListByTinAndPaymentId(
        [Required(AllowEmptyStrings = false), MaxLength(9)] string providerTin,
        [Required(AllowEmptyStrings = false), MaxLength(100)] string paymentId,
        CancellationToken token = default)
    {
        try
        {
            var paymentDetails = await _mediator.Send(new GetPaymentByProviderTinAndPaymentIdQuery(providerTin, paymentId), token).ConfigureAwait(false);
            if (paymentDetails is null)
            {
                var sanitizedPaymentId = paymentId.Replace("\n", "").Replace("\r", "");
                var sanitizedProviderTin = providerTin.Replace("\n", "").Replace("\r", "");
                _logger.LogInformation("Payment not found for {ProviderTin} and {PaymentId} ", sanitizedProviderTin, sanitizedPaymentId);
                return NotFound();
            }
            return await GetDocumentsList(paymentDetails.TransactionId, token);
        }
        catch (ItemNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            var sanitizedPaymentId = paymentId.Replace("\n", "").Replace("\r", "");
            var sanitizedProviderTin = providerTin.Replace("\n", "").Replace("\r", "");
            _logger.LogError(ex, "Attempt to get Payment failed with unexpected error for {ProviderTin} and {PaymentId} ", sanitizedProviderTin, sanitizedPaymentId);
            return StatusCode(500);
        }
    }

    /// <summary>
    /// Gets the documents associated with the transaction.
    /// </summary>
    /// <param name="transactionId">The ID of the transaction to retrieve documents for.</param>
    /// <param name="token">A token that can be used to cancel the work.</param>
    /// <response code="200">If documents are retrieved successfully for the transaction.</response>
    /// <response code="404">If cannot find documents for the transactionId.</response>
    [HttpGet("{transactionId}")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status404NotFound)]
    public async Task<ActionResult<List<DocumentDto>>> GetDocumentsList(long transactionId, CancellationToken token = default)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object> { ["TransactionId"] = transactionId });

        try
        {
            var documents = await _mediator.Send(new GetDocumentsByTransactionIdQuery(transactionId), token).ConfigureAwait(false);
            if (documents == null)
            {
                return NotFound();
            }
            return _mapper.Map<List<DocumentDto>>(documents);
        }
        catch (NotAuthorizedException ex)
        {
            _logger.LogInformation(ex, "Document list - SECURITY - Client tried to get list of documents for transaction [{TransactionId}] when they do not have access to it.", transactionId);
            return NotFound();
        }
    }

    /// <summary>
    /// Gets the document with the given id.
    /// </summary>
    /// <param name="transactionId">The ID of the transaction the document is associated with.</param>
    /// <param name="documentId">The ID of the document to retrieve.</param>
    /// <param name="token">A token that can be used to cancel the work.</param>
    /// <response code="200">If documents are retrieved successfully for the transaction.</response>
    /// <response code="404">If the document cannot be found.</response>
    [HttpGet("{transactionId}/{documentId}/download")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status404NotFound)]
    [Produces("application/octet-stream", "application/json", "text/plain")]
    public async Task<ActionResult> GetDocument(long transactionId, long documentId, CancellationToken token = default)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object> { ["TransactionId"] = transactionId, ["DocumentId"] = documentId });
        try
        {
            var document = await _mediator.Send(new DownloadDocumentByIdQuery(transactionId, documentId), token).ConfigureAwait(false);
            if (document == null)
            {
                return NotFound();
            }

            return File(
                document.Stream,
                document.ContentType,
                document.FileName);
        }
        catch (NotAuthorizedException ex)
        {
            _logger.LogInformation(ex, "Document lookup - SECURITY - Client tried to download document [{TransactionId} - {DocumentId}] when they do not have access to it.", transactionId, documentId);
            return NotFound();
        }
        catch (DocumentNotInArchiveException ex)
        {
            _logger.LogInformation(ex, "Document lookup - Document {DocumentId} does not exist for Transaction {TransactionId}", documentId, transactionId);
            return NotFound();
        }
    }

    /// <summary>
    /// Gets the Payment Remittance Advice document for the passed in Transaction Id
    /// </summary>
    /// <param name="transactionId">The ID of the transaction the document is associated with.</param>
    /// <param name="token">A token that can be used to cancel the work.</param>
    /// <response code="200">If documents are retrieved successfully for the transaction.</response>
    /// <response code="404">If the document cannot be found.</response>
    [HttpGet("{transactionId}/PRA/download")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status404NotFound)]
    [Produces("application/octet-stream", "application/json", "text/plain")]
    [Obsolete("The alternative PRA download endpoint that accepts the payer and sub-payer information should be used ")]
    public Task<ActionResult> GetPraDocument(long transactionId, CancellationToken token = default)
    {
        return GetPraDocumentByTransactionId(transactionId, false, token);
    }

    /// <summary>
    /// Gets the Payment Remittance Advice document for the passed in payer, sub-payer, and payment id
    /// </summary>
    /// <param name="payerId">The ID of the payer the transaction is associated with. (Hint: This is not the client code.)</param>
    /// <param name="subPayerId">The ID of the sub-payer the transaction is associated with. (Hint: This is not the billing entity code.)</param>
    /// <param name="paymentId">The ID of the transaction the document is associated with (Transaction ID or Unconsolidated Payment Number/UserKey).</param>
    /// <param name="masked">True if requesting a masked version of the PRA, False if clear text version.</param>
    /// <param name="token">A token that can be used to cancel the work.</param>
    /// <response code="200">If documents are retrieved successfully for the transaction.</response>
    /// <response code="400">If the parameters are invalid, the sub-payer can't be found, or the payment for the sub-payer can't be found.</response>
    /// <response code="404">If the document cannot be found.</response>
    [HttpGet("{payerId}/{subPayerId}/{paymentId}/PRA/download")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status400BadRequest)]
    [ProducesResponseType(Status404NotFound)]
    [Produces("application/octet-stream", "application/json", "text/plain")]
    public async Task<ActionResult> GetPraDocument(
        [Required(AllowEmptyStrings = false), MaxLength(10)] string payerId,
        [Required(AllowEmptyStrings = false), MaxLength(10)] string subPayerId,
        [Required(AllowEmptyStrings = false), MaxLength(49)] string paymentId,
        bool masked = false,
        CancellationToken token = default)
    {
        using var loggingScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["PaymentId"] = paymentId,
            ["PayerId"] = payerId,
            ["SubPayerId"] = subPayerId,
            ["Masked"] = masked
        });

        OptumPrsConfigurationDto prsConfiguration = null;

        if (!payerId.Equals("VPAY5", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                prsConfiguration = await _mediator.Send(new GetOptumPrsConfiguration.Query(payerId, subPayerId), token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, "PRA Download - Invalid payer info for transaction [{PaymentId}]: payerId and/or subPayerId are incorrect.",
                    paymentId.Replace(Environment.NewLine, string.Empty));

                // if the payer is invalid or the sub-payer is invalid that's an error
                var key = $"{nameof(payerId)}/{nameof(subPayerId)}";
                ModelState.AddModelError(key, ex.Message);
                return BadRequest(ModelState);
            }
        }

        bool isPrsEnabled = prsConfiguration?.IsPrsEnabled ?? false;

        long transactionId;
        if (isPrsEnabled)
        {
            // if the payer is enabled for PRS, the value we have been given as the Payment Id is the
            // Unconsolidated Payment Number/UserKey

            // if the payer is enabled for USER KEY PREFIX, the payment number must be pre-pended with
            // the sub-payer Id to ensure uniqueness across all billing entities.
            var originCode = prsConfiguration.OriginCode;
            var clientCode = prsConfiguration.ClientCode;
            var originalPaymentId = paymentId;

            // Add prefix to the payment id if needed based on the client configuration. This is necessary
            // to ensure we can find the transaction associated with the payment id when the client uses
            // the same payment numbers across different sub-payers.
            paymentId = await AddSubPayerIdPrefixIfNeeded(originCode, clientCode, subPayerId, originalPaymentId, token).ConfigureAwait(false);

            // We need to translate the unconsolidated payment number to the transaction id.
            var result = await ConvertUnconsolidatedPaymentNumberToTransactionId(paymentId, subPayerId, token).ConfigureAwait(false);

            // If the response is not null, that means we were not able to find the transaction id.
            if (result.Response != null)
            {
                // If the original payment id is the same as the prefixed one, that means the client doesn't
                // have prefix requirement but we still couldn't find the transaction, so we can return not
                // found right away without trying the original payment id.
                if (paymentId == originalPaymentId)
                {
                    _logger.LogInformation("PRA Download - Missing transaction for paymentId [{PaymentId}].",
                        paymentId.Replace(Environment.NewLine, string.Empty));
                    return result.Response;
                }

                _logger.LogInformation("PRA Download - Missing transaction for prefixed paymentId [{PaymentId}]. Attempting with original payment id [{OriginalPaymentId}].",
                    paymentId.Replace(Environment.NewLine, string.Empty), originalPaymentId.Replace(Environment.NewLine, string.Empty));

                // We need to translate the original payment number to the transaction id for cases when
                // a prefix setting was added after the payment was processed.
                result = await ConvertUnconsolidatedPaymentNumberToTransactionId(originalPaymentId, subPayerId, token).ConfigureAwait(false);

                // If the response is not null, that means we were not able to find the transaction id.
                if (result.Response != null)
                {
                    _logger.LogInformation("PRA Download - Missing transaction for original paymentId [{OriginalPaymentId}].",
                        originalPaymentId.Replace(Environment.NewLine, string.Empty));
                    return result.Response;
                }
                // If the original payment number works but the prefixed one doesn't, that means the payment
                // was processed before the prefix requirement was added, so we can proceed with the original
                // payment number.
                paymentId = originalPaymentId;
            }
            transactionId = result.TransactionId.Value;
        }
        else if (!long.TryParse(paymentId, out transactionId))
        {
            _logger.LogInformation("PRA Download - Invalid transaction for provided paymentId [{PaymentId}].",
                paymentId.Replace(Environment.NewLine, string.Empty));

            // if we get here, the payer id is VPAY5 or other payers not enabled for PRS,
            // and the number we were given *should* be a transaction id
            // if it can't be parsed, that's an error
            ModelState.AddModelError(nameof(paymentId), $"When the {nameof(payerId)} is VPAY5 or is not enabled for PRS, the {nameof(paymentId)} should be a numeric TransactionId.");
            return BadRequest(ModelState);
        }

        return await GetPraDocumentByTransactionId(transactionId, masked, token);
    }

    /// <summary>
    /// Gets the Payment Remittance Advice document for the passed in Transaction Id
    /// </summary>
    /// <param name="transactionId">The ID of the transaction the document is associated with.</param>
    /// <param name="token">A token that can be used to cancel the work.</param>
    /// <response code="200">If documents are retrieved successfully for the transaction.</response>
    /// <response code="404">If the document cannot be found.</response>
    [HttpGet("{transactionId}/835/download")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status404NotFound)]
    [Produces("application/json", "text/plain")]
    public async Task<ActionResult> Get835Document(long transactionId, CancellationToken token = default)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object> { ["TransactionId"] = transactionId });
        try
        {
            var document = await _mediator.Send(new Download835FileByTransactionIdQuery(transactionId), token).ConfigureAwait(false);
            if (document == null)
            {
                _logger.LogWarning("835 Download - Could not find Document for Transaction {TransactionId}", transactionId);
                return NotFound(Edi835Statuses.TransactionMissing);
            }

            return File(
                document.Stream,
                document.ContentType,
                document.FileName);
        }
        catch (NotAuthorizedException ex)
        {
            _logger.LogInformation(ex, "835 Download - SECURITY - Client tried to download document [{TransactionId}] when they do not have access to it.", transactionId);
            return NotFound(Edi835Statuses.TransactionMissing);
        }
        catch (ItemNotFoundException ex)
        {
            if (ex.Message == Edi835Statuses.TransactionMissing)
            {
                _logger.LogWarning("835 Download - Could not find Document for Transaction {TransactionId} when they are expected", transactionId);
            }
            else
            {
                _logger.LogInformation("835 Download - Could not find Document for Transaction {TransactionId}, with message {Reason}", transactionId, ex.Message);
            }
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "835 Download - Could not find Document or Transaction for Transaction {TransactionId}, with message {Reason}", transactionId, ex.Message);
            return NotFound();
        }
    }

    /// <summary>
    /// Returns VPay transactionID along with other details when a payment in VPay matches the search criteria in request for the passed in Start date, End Date and Payment id
    /// </summary>
    /// <param name="request">Contains the Payment Id, start date and end date</param>
    /// <param name="token">A token that can be used to cancel the work.</param>
    /// <response code="200">If Transactions are retrieved successfully .</response>
    /// <response code="400">If the parameters are invalid, the transactions for the given parameters can't be found.</response>
    /// <response code="404">If the transactions cannot be found.</response>
    [HttpGet("transactionID/search")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status400BadRequest)]
    [ProducesResponseType(Status404NotFound)]
    [Produces("application/json")]
    public async Task<ActionResult<Pagination<TransactionDetails>>> GetTransactionIds(
        [Required][FromQuery] TransactionSearchDto request,
        CancellationToken token = default)
    {
        using var loggingScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["PaymentId"] = request.PaymentId,
            ["StartDate"] = request.StartDate,
            ["EndDate"] = request.EndDate,
            ["TaxId"] = request.ProviderTin
        });

        string paymentId = request.PaymentId;
        DateTime startDate = (DateTime)request.StartDate;
        DateTime endDate = (DateTime)AdjustEndDate(request.EndDate);
        string taxId = request.ProviderTin;

        try
        {
            var response = await _mediator
                .Send(new GetTransactionsByDateRangeQuery(paymentId, startDate, endDate, taxId, request.Page, request.PageSize), token)
                .ConfigureAwait(false);
            return Ok(response);
        }
        catch (ItemNotFoundException)
        {
            return NotFound();
        }
        catch (ApiException apiEx)
        {
            if (apiEx.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogError(apiEx, "Unauthorized - {Message}.", apiEx.Message);
                return Unauthorized(apiEx.Message);
            }
            _logger.LogError(apiEx, "Exception occurred - {Message}.", apiEx.Message);
            return BadRequest(apiEx.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred - {Message}.", ex.Message);
            return StatusCode(500);
        }
    }

    private async Task<ActionResult> GetPraDocumentByTransactionId(long transactionId, bool masked, CancellationToken token = default)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object> { ["TransactionId"] = transactionId });
        try
        {
            var document = await _mediator.Send(new DownloadPraFileByTransactionIdQuery(transactionId, masked), token).ConfigureAwait(false);
            if (document == null)
            {
                return NotFound(DocumentPraStatuses.TransactionMissing);
            }

            return File(
                document.Stream,
                document.ContentType,
                document.FileName);
        }
        catch (NotAuthorizedException ex)
        {
            _logger.LogInformation(ex, "PRA lookup - SECURITY - Client tried to download document [{TransactionId}] when they do not have access to it.", transactionId);
            return NotFound(DocumentPraStatuses.TransactionMissing);
        }
        catch (ItemNotFoundException ex)
        {
            _logger.LogInformation(ex, "PRA lookup - Could not find Document for Transaction {TransactionId}", transactionId);
            return NotFound(ex.Message);
        }
    }

    private async Task<(long? TransactionId, ActionResult Response)> ConvertUnconsolidatedPaymentNumberToTransactionId(
        string paymentId,
        string subPayerId,
        CancellationToken cancellationToken)
    {
        string cacheKey = $"TXID:{subPayerId}:{paymentId}";
        var context = new Context(
            cacheKey,
            new Dictionary<string, object>
            {
                [PolicyContextItems.Logger] = _logger
            });

        try
        {
            var transactionId = await _cachePolicy.ExecuteAsync(async (context, token) =>
            {
                var transactionInfo = await _mediator
                    .Send(new PaymentNumberToTransactionId.Query(subPayerId, paymentId), token).ConfigureAwait(false);

                return transactionInfo.TransactionId;
            }, context, cancellationToken);

            return (TransactionId: transactionId, Response: null);
        }
        catch (RedisConnectionException ex)
        {
            _logger.LogError(ex, "Redis connection failed while trying to retrieve transaction id for paymentId [{PaymentId}] and subPayerId [{SubPayerId}].", paymentId.Replace(Environment.NewLine, string.Empty), subPayerId.Replace(Environment.NewLine, string.Empty));

            var transactionInfo = await _mediator
                .Send(new PaymentNumberToTransactionId.Query(subPayerId, paymentId), cancellationToken)
                .ConfigureAwait(false);

            var transactionId = transactionInfo?.TransactionId;

            return (TransactionId: transactionId, Response: null);
        }
        catch (ItemNotFoundException ex)
        {
            return (TransactionId: null, NotFound(ex.Message));
        }
    }

    /// <summary>
    /// Determine if an unconsolidated payment number (user key) for a given PRS client (payer)
    /// should be pre-pended with a prefix to make it unique across all other billing entities
    /// for the same client. This adjustment is important because clients that use the same
    /// unconsolidated payment number (user key) with different sub-payers (billing entities)
    /// have their sub-payer Id prefixed to the user key to ensure uniqueness. Therefore the query
    /// to retrieve the payment associated document requires the pre-pended unconsolidated payment number
    /// in order to succeed. Result with prefix: {SubPayerID}_{UserKey}, and without prefix: {UserKey}
    /// </summary>
    /// <param name="originCode">The origin code of the payer.</param>
    /// <param name="clientCode">The client code of the payer.</param>
    /// <param name="subPayerId">The sub-payer Id for the corresponding billing entity code
    /// that may be used as a prefix if needed.</param>
    /// <param name="paymentId">The original unconsolidated payment number
    /// (without the pre-pended sub-payer Id).</param>
    /// <param name="cancellationToken">A token to cancel the work.</param>
    /// <returns>The unconsolidated payment number (user key) pre-pended with the sub-payer Id
    /// when the PRS client is configured for prefix use;
    /// The original unconsolidated payment number otherwise.</returns>
    private async Task<string> AddSubPayerIdPrefixIfNeeded(
        string originCode,
        string clientCode,
        string subPayerId,
        string paymentId,
        CancellationToken cancellationToken)
    {
        var clientConfig = await _mediator
            .Send(new GetClientBoardingConfiguration.Query("PREFIX_USERKEY", originCode, clientCode), cancellationToken)
            .ConfigureAwait(false);

        var prefixConfig = _mapper.Map<ClientPrefixUserKeyConfigurationDto>(clientConfig);

        // If prefix configuration is active and enabled for the given client code and billing entity code...
        if (prefixConfig.IsPrefixRequired)
        {
            // Concatenate unconsolidated payment number as SubPayerID_UserKey
            return string.Concat(subPayerId, "_", paymentId);
        }

        return paymentId;
    }

    /// <summary>
    /// Adjust end date to include until the end of the day when the time component is not provided.
    /// </summary>
    /// <param name="endDate">The end date to adjust.</param>
    /// <returns>The end date with the time set until the end of the day.</returns>
    private static DateTime? AdjustEndDate(DateTime? endDate)
    {
        if (!endDate.HasValue || endDate.Value.TimeOfDay.Ticks > 0)
        {
            return endDate;
        }

        return endDate.Value.AddDays(1).AddSeconds(-1);
    }

    /// <summary>
    /// Returns VPay transactionID along with other details when a payment in VPay matches the search criteria in request for the passed in Start date, End Date , TRN02 and Providr TIN
    /// </summary>
    /// <param name="request">Contains the Provider Tin, TRN02, start date and end date</param>
    /// <param name="token">A token that can be used to cancel the work.</param>
    /// <response code="200">If Transactions are retrieved successfully .</response>
    /// <response code="400">If the parameters are invalid, the transactions for the given parameters can't be found.</response>
    /// <response code="404">If the transactions cannot be found.</response>
    [HttpGet("transactionID/TRN02search")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status400BadRequest)]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType(Status500InternalServerError)]
    [Produces("application/json")]
    public async Task<ActionResult<Pagination<TransactionsTrn02DetailsDto>>> GetTransactionIdsByTRN02(
        [Required][FromQuery] Trn02SearchRequest request,
        CancellationToken token = default)
    {
        using var loggingScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["TRN02"] = request.Trn02,
            ["StartDate"] = request.StartDate,
            ["EndDate"] = request.EndDate,
            ["TaxId"] = request.ProviderTin
        });

        string trn02 = request.Trn02;
        DateTime startDate = (DateTime)request.StartDate;
        DateTime endDate = (DateTime)AdjustEndDate(request.EndDate);
        string taxId = request.ProviderTin;

        if (startDate > endDate)
        {
            ModelState.AddModelError(nameof(request.StartDate), "Start date cannot be greater than end date.");
            return BadRequest(ModelState);
        }
        try
        {

            var response = await _mediator
                .Send(new GetTransactionsByTrn02.Query(request.StartDate,
                request.EndDate,
                request.Trn02,
                request.ProviderTin,
            request.Page,
            request.PageSize), token)
                .ConfigureAwait(false);

            if (response is null)
            {
                _logger.Log(LogLevel.Information, "No transactions found for the given search criteria.");
                return NotFound();
            }
            if (response.Items?.Count() == 0)
            {
                return NotFound();
            }

            var filteredItems = response.Items
                .Where(item => _userInfo.CanAccess(item.ClientID, item.BillingEntity))
                .Select(item => new TransactionsTrn02DetailsDto
                {
                    VpayTransactionId = item.VpayTransactionId,
                    PaymentId = item.PaymentId,
                    PaymentType = item.PaymentType,
                    PaymentDate = item.PaymentDate,
                    ProviderTin = item.ProviderTin,
                    Trn02 = item.Trn02
                })
                .ToList();
            if (filteredItems.Count == 0)
            {
                _logger.Log(LogLevel.Information, "User ID associated with client can't access transactions");
                return NotFound();
            }

            var paginatedResponse = new Pagination<TransactionsTrn02DetailsDto>
            {
                CurrentPage = response.CurrentPage,
                PageSize = response.PageSize,
                TotalRows = filteredItems.Count,
                TotalPages = (int)Math.Ceiling((double)filteredItems.Count / response.PageSize),
                Items = filteredItems
            };

            return Ok(paginatedResponse);
        }
        catch (ItemNotFoundException e)
        {
            _logger.LogError(e, "No transactions found for the given search criteria.");
            return NotFound();
        }

        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred - {Message}.", ex.Message);
            var key = $"{nameof(trn02)}";
            ModelState.AddModelError("", ex.Message);
            return StatusCode(500);
        }

    }
}
