using System.Globalization;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Vpay.Gateway.Common.Vpay.Models;
using VPay.Extensions.Exceptions;
using VPay.Gateway.Api.Configuration;
using VPay.Gateway.Api.Models;
using VPay.Gateway.Api.MvcCustomizations;
using VPay.Gateway.Application;
using VPay.Gateway.Common.Documents;
using VPay.Gateway.Common.MemberClaims;
using VPay.Gateway.Common.PlatformConfig;
using VPay.Gateway.Common.RestPayments;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace VPay.Gateway.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[FeatureFlag("IsZetaEnabled")]
public class VendorPaymentsController : ControllerBase
{
    private readonly IUserInfo _userInfo;
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly ILogger<VendorPaymentsController> _logger;

    public VendorPaymentsController(
        IUserInfo userInfo,
        IMediator mediator,
        IMapper mapper,
        ILogger<VendorPaymentsController> logger)
    {
        _userInfo = userInfo ?? throw new ArgumentNullException(nameof(userInfo));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet("payment-details")]
    [ProducesResponseType(typeof(TransactionSearchResponseVendor), Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), Status404NotFound)]
    [Authorize(Roles = "B2C Get Payment Data")]
    public async Task<ActionResult<TransactionSearchResponseVendor>> GetPaymentDetail(
        [FromQuery] string? preTransactionId,
        [FromQuery] long? transactionId,
        CancellationToken cancellationToken = default)
    {
        string errorTitle = string.Empty;
        string errorMessage = string.Empty;
        string logAction = nameof(GetPaymentDetail);
        string logPreTransactionId = preTransactionId?.Replace(Environment.NewLine, string.Empty) ?? "N/A";
        string logTransactionId = transactionId.HasValue ? transactionId.Value.ToString(CultureInfo.InvariantCulture).Replace(Environment.NewLine, string.Empty) : "N/A";

        try
        {
            long vpayTransId = transactionId ?? 0;

            // Remove literal prefix from preTransactionId if present in first character
            if (preTransactionId?.Length > 0 && char.IsAsciiLetter(preTransactionId[0]))
            {
                preTransactionId = preTransactionId.Substring(1);
            }
            long preTransId = long.TryParse(preTransactionId, out var tempPreTransId) ? tempPreTransId : 0;

            // Verify that if preTransactionId is provided, and stripped of any optional literal first character,
            // it is a valid long value greater than 0
            if (!string.IsNullOrEmpty(preTransactionId) && preTransId <= 0)
            {
                errorMessage = "Invalid PreTransactionId: must be numeric and greater than 0 after being stripped of any optional literal first character";
                return ValidationProblem(CreateValidationDetails(nameof(preTransactionId), errorMessage));
            }

            // Verify that if transactionId is provided, it is greater than 0
            if (transactionId.HasValue && transactionId <= 0)
            {
                errorMessage = "TransactionId must be greater than 0";
                return ValidationProblem(CreateValidationDetails(nameof(transactionId), errorMessage));
            }

            // Validate that at least one of preTransactionId or transactionId is provided and valid
            if (vpayTransId <= 0 && preTransId <= 0)
            {
                errorMessage = "Either PreTransactionId or TransactionId must be provided and be valid";
                var details = CreateValidationDetails(new Dictionary<string, string>
                {
                    [nameof(preTransactionId)] = errorMessage,
                    [nameof(transactionId)] = errorMessage
                });
                return ValidationProblem(details);
            }

            var user = _userInfo.DisplayName;
            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["DisplayName"] = user,
                ["PreTransactionId"] = logPreTransactionId,
                ["TransactionId"] = logTransactionId,
                ["Action"] = logAction
            }))
            {
                // If transactionId is not valid, use preTransactionId to fetch it
                if (vpayTransId == 0)
                {
                    try
                    {
                        GetTransactionbyPreTransIdQuery queryTransaction = new(preTransactionId);
                        vpayTransId = await _mediator.Send(queryTransaction, cancellationToken).ConfigureAwait(false);
                    }
                    catch (InvalidOperationException ex)
                    {
                        errorTitle = "Not Found";
                        errorMessage = "Not transaction found for given PreTransactionId";
                        _logger.LogWarning(ex, "{Action} - {ErrorMessage} getting payment detail for Transaction {TransactionId} by {DisplayName}", logAction, errorMessage, logTransactionId, user);
                        return Problem(detail: errorMessage, statusCode: 404, title: errorTitle, type: "https://httpstatuses.io/404");
                    }
                }

                // If transactionId is still not valid, return a bad request response
                if (vpayTransId == 0)
                {
                    errorTitle = "Invalid Input";
                    errorMessage = "Unable to retrieve Transaction for given PreTransactionId";
                    _logger.LogWarning("{Action} - {ErrorMessage} getting payment detail for Transaction {TransactionId} by {DisplayName}", logAction, errorMessage, logTransactionId, user);
                    return Problem(detail: errorMessage, statusCode: 400, title: errorTitle, type: "https://httpstatuses.io/400");
                }

                // Get transaction info to validate the client code and billing entity code associated with the transaction for authorization and access control
                var transactionInfo = await _mediator.Send(new TransactionInfo.Query(vpayTransId), cancellationToken).ConfigureAwait(false);

                // If transaction info is not found for the given transaction id, return a not found response
                if (transactionInfo.StatusCode == HttpStatusCode.NotFound)
                {
                    errorTitle = "Not Found";
                    errorMessage = "No transaction found for the given TransactionId";
                    _logger.LogWarning("{Action} - {ErrorMessage} getting payment detail for Transaction {TransactionId} by {DisplayName}", logAction, errorMessage, logTransactionId, user);
                    return Problem(detail: errorMessage, statusCode: 404, title: errorTitle, type: "https://httpstatuses.io/404");
                }

                // If transaction info query is unsuccessful, return a bad request response with the error message
                if (!transactionInfo.IsSuccessStatusCode || transactionInfo.Content == null)
                {
                    errorTitle = "Invalid Input";
                    errorMessage = "Unable to retrieve transaction for the given TransactionId or PreTransactionId";
                    _logger.LogWarning("{Action} - {ErrorMessage} getting payment detail for Transaction {TransactionId} by {DisplayName}", logAction, errorMessage, logTransactionId, user);
                    return Problem(detail: errorMessage, statusCode: 400, title: errorTitle, type: "https://httpstatuses.io/400");
                }

                // If user doesn't have access to the client code and billing entity code associated with the transaction, return an unauthorized response
                if (!_userInfo.CanAccess(transactionInfo.Content.ClientCode, transactionInfo.Content.BillingEntityCode))
                {
                    errorTitle = "Unauthorized Request";
                    errorMessage = "User not configured for provided Payer and/or Sub-Payer";
                    _logger.LogWarning("{Action} - {ErrorMessage} getting payment detail for Transaction {TransactionId} by {DisplayName}", logAction, errorMessage, logTransactionId, user);
                    return Problem(detail: errorMessage, statusCode: 401, title: errorTitle, type: "https://httpstatuses.io/401");
                }

                GetPreTransIdbyTransactionQuery queryPreTransaction = new(vpayTransId);
                string vpayPreTransId = await _mediator.Send(queryPreTransaction, cancellationToken).ConfigureAwait(false);
                if (string.IsNullOrEmpty(vpayPreTransId) && (!transactionId.HasValue || transactionId == 0))
                {
                    errorTitle = "Invalid Input";
                    errorMessage = "Unable to determine PreTransactionId from TransactionId";
                    _logger.LogWarning("{Action} - {ErrorMessage} getting payment detail for Transaction {TransactionId} by {DisplayName}", logAction, errorMessage, logTransactionId, user);
                    return Problem(detail: errorMessage, statusCode: 400, title: errorTitle, type: "https://httpstatuses.io/400");
                }

                // Get vendor payment detail
                GetVendorPaymentDetailQuery query = new(vpayPreTransId, vpayTransId);
                var response = await _mediator.Send(query, cancellationToken).ConfigureAwait(false);

                if (response == null)
                {
                    errorTitle = "Invalid Input";
                    errorMessage = "Unable to retrieve Payment Detail for the given TransactionId or PreTransactionId";
                    _logger.LogWarning("{Action} - {ErrorMessage} getting payment detail for Transaction {TransactionId} by {DisplayName}", logAction, errorMessage, logTransactionId, user);
                    return Problem(detail: errorMessage, statusCode: 404, title: errorTitle, type: "https://httpstatuses.io/404");
                }
                return Ok(response);
            }
        }
        catch (FluentValidation.ValidationException ex)
        {
            var details = ex.AsProblemDetails();
            _logger.LogWarning(ex, "{Action} - Validation failed. " +
                "PreTransactionId: {PreTransactionId}, TransactionId: {TransactionId}. " +
                "Validation Errors: {ErrorMessage}", logAction, logPreTransactionId, logTransactionId,
                string.Join("; ", details.Errors.Select(e => $"{e.Key}: {string.Join(", ", e.Value)}")));
            return ValidationProblem(details);
        }
        catch (ItemNotFoundException ex)
        {
            errorTitle = "Not Found";
            errorMessage = ex.InnerException?.Message ?? ex.Message ?? "An unexpected not found error occurred";
            _logger.LogError(ex, "{Action} - {ErrorMessage} getting payment detail. PreTransactionId: {PreTransactionId}, Transaction Id: {TransactionId}", logAction, errorMessage, logPreTransactionId, logTransactionId);
            return Problem(detail: errorMessage, statusCode: 404, title: errorTitle, type: "https://httpstatuses.io/404");
        }
        catch (BadRequestException ex)
        {
            errorTitle = "Bad Request";
            errorMessage = ex.InnerException?.Message ?? ex.Message ?? "An unexpected bad request error occurred";
            _logger.LogError(ex, "{Action} - {ErrorMessage} getting payment detail. PreTransactionId: {PreTransactionId}, Transaction Id: {TransactionId}", logAction, errorMessage, logPreTransactionId, logTransactionId);
            return Problem(detail: errorMessage, statusCode: 400, title: errorTitle, type: "https://httpstatuses.io/400");
        }
        catch (ConflictException ex)
        {
            errorTitle = "Bad Request";
            errorMessage = ex.InnerException?.Message ?? ex.Message ?? "An unexpected data conflict error occurred";
            _logger.LogError(ex, "{Action} - {ErrorMessage} getting payment detail. PreTransactionId: {PreTransactionId}, Transaction Id: {TransactionId}", logAction, errorMessage, logPreTransactionId, logTransactionId);
            return Problem(detail: errorMessage, statusCode: 409, title: errorTitle, type: "https://httpstatuses.io/409");
        }
        catch (NotAuthorizedException ex)
        {
            errorTitle = "Forbidden Request";
            errorMessage = "User not allowed to perform this action";
            _logger.LogError(ex, "{Action} - {ErrorMessage} getting payment detail. PreTransactionId: {PreTransactionId}, Transaction Id: {TransactionId}", logAction, errorMessage, logPreTransactionId, logTransactionId);
            return Problem(detail: errorMessage, statusCode: 403, title: errorTitle, type: "https://httpstatuses.io/403");
        }
        catch (Exception ex)
        {
            errorTitle = "Unexpected Error";
            errorMessage = ex.InnerException?.Message ?? ex.Message ?? "An unexpected error occurred";
            _logger.LogError(ex, "{Action} - {ErrorMessage} getting payment detail. PreTransactionId: {PreTransactionId}, Transaction Id: {TransactionId}", logAction, errorMessage, logPreTransactionId, logTransactionId);
            return Problem(detail: errorMessage, statusCode: 400, title: errorTitle, type: "https://httpstatuses.io/400");
        }
    }

    /// <summary>
    /// Voids the payment
    /// </summary> 
    /// <param name="request">Entity containing Requester Name and TransactionId for a void payment request</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>OK if successful</returns>
    [HttpPost("stop-payment")]
    [ProducesResponseType(typeof(UnloadVendorPaymentResponse), Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), Status400BadRequest)]
    [ProducesResponseType(typeof(void), Status404NotFound)]
    [Authorize(Roles = "B2C Update Payment")]
    public async Task<ActionResult> StopPayment([FromBody] VendorStopPaymentRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var user = _userInfo.DisplayName;
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["DisplayName"] = user,
            ["TransactionId"] = request.TransactionId
        }))
        {
            var transactionInfo = await _mediator.Send(new TransactionInfo.Query(request.TransactionId), cancellationToken)
                .ConfigureAwait(false);

            if (transactionInfo.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Not Found response in Transaction Info for Transaction {TransactionId} by {DisplayName}",
                    request.TransactionId, user);
                return NotFound();
            }

            if (!transactionInfo.IsSuccessStatusCode || transactionInfo.Content == null)
            {
                _logger.LogError(transactionInfo.Error,
                    "Unknown response from Transaction Info for Transaction {TransactionId} by {DisplayName}", request.TransactionId, user);
                return StatusCode(500);
            }

            if (!_userInfo.CanAccess(transactionInfo.Content.ClientCode, transactionInfo.Content.BillingEntityCode))
            {
                _logger.LogWarning(
                    "Attempt to Void payment denied due to missing role allowing it in Void Payment for Transaction {TransactionId} by {DisplayName}",
                    request.TransactionId, user);
                return Unauthorized();
            }

            var response = await _mediator.Send(new StopVendorPayment.Query(user, request.TransactionId), cancellationToken)
                .ConfigureAwait(false);

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    _logger.LogInformation("Payment Voided Successfully for Transaction {TransactionId} by {DisplayName}",
                        request.TransactionId, user);
                    return Ok(response.Content);

                case HttpStatusCode.BadRequest:
                    _logger.LogWarning("Bad Request response in Void Payment for Transaction {TransactionId} by {DisplayName}",
                        request.TransactionId, user);

                    try
                    {
                        var errorResponse = response.Content ??
                                            JsonConvert.DeserializeObject<UnloadVendorPaymentResponse>(
                                                response.Error?.Content ?? "");

                        var errors = new Dictionary<string, string[]>
                        {
                            ["Reason"] = new[] { errorResponse?.ReasonCode, errorResponse?.ReasonDesc },
                            ["Response"] = new[] { errorResponse?.ResponseCode, errorResponse?.ResponseDesc },
                            ["Success"] = new[] { errorResponse?.SuccessCode, errorResponse?.SuccessDesc }
                        };
                        var validationError = new ValidationProblemDetails(errors);
                        return ValidationProblem(validationError);
                    }
                    catch (JsonSerializationException)
                    {
                        return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
                        {
                            ["Error"] = new[] { response.Error?.Content ?? "An unknown error occurred" }
                        }));
                    }

                case HttpStatusCode.Unauthorized:
                    _logger.LogWarning("Unauthorized response in Void Payment for Transaction {TransactionId} by {DisplayName}",
                        request.TransactionId, user);
                    return Unauthorized();

                case HttpStatusCode.NotFound:
                    _logger.LogWarning("Not Found response in Void Payment for Transaction {TransactionId} by {DisplayName}",
                        request.TransactionId, user);
                    return NotFound();

                default:
                    _logger.LogError(response.Error,
                        "Unknown response from Void Payment for Transaction {TransactionId} by {DisplayName}", request.TransactionId, user);
                    return StatusCode(500);
            }
        }
    }

    /// <summary>
    /// Voids the payment
    /// </summary>
    /// <param name="request">Entity containing Requester Name and TransactionId for a stop payment request</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>OK if successful</returns>
    [HttpPost("void-payment")]
    [ProducesResponseType(typeof(UnloadVendorPaymentResponse), Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), Status400BadRequest)]
    [ProducesResponseType(typeof(void), Status404NotFound)]
    [Authorize(Roles = "B2C Update Payment")]
    public async Task<ActionResult> VoidPayment([FromBody] VendorVoidPaymentRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var user = _userInfo.DisplayName;
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["DisplayName"] = user,
            ["TransactionId"] = request.TransactionId
        }))
        {
            var transactionInfo = await _mediator.Send(new TransactionInfo.Query(request.TransactionId), cancellationToken)
                .ConfigureAwait(false);

            if (transactionInfo.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Not Found response in Transaction Info for Transaction {TransactionId} by {DisplayName}",
                    request.TransactionId, user);
                return NotFound();
            }

            if (!transactionInfo.IsSuccessStatusCode || transactionInfo.Content == null)
            {
                _logger.LogError(transactionInfo.Error,
                    "Unknown response from Transaction Info for Transaction {TransactionId} by {DisplayName}", request.TransactionId, user);
                return StatusCode(500);
            }

            if (!_userInfo.CanAccess(transactionInfo.Content.ClientCode, transactionInfo.Content.BillingEntityCode))
            {
                _logger.LogWarning(
                    "Attempt to Void payment denied due to missing role allowing it in Void Payment for Transaction {TransactionId} by {DisplayName}",
                    request.TransactionId, user);
                return Unauthorized();
            }

            var response = await _mediator.Send(new UnloadVendorPayment.Query(user, request.TransactionId, null), cancellationToken)
                .ConfigureAwait(false);

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    _logger.LogInformation("Vendor Payment Voided Successfully for Transaction {TransactionId} by {DisplayName}",
                        request.TransactionId, user);
                    return Ok(response.Content);

                case HttpStatusCode.BadRequest:
                    _logger.LogWarning("Bad Request response in Void Vendor Payment for Transaction {TransactionId} by {DisplayName}",
                        request.TransactionId, user);

                    try
                    {
                        var errorResponse = response.Content ??
                                            JsonConvert.DeserializeObject<UnloadVendorPaymentResponse>(
                                                response.Error?.Content ?? "");

                        var errors = new Dictionary<string, string[]>
                        {
                            ["Reason"] = new[] { errorResponse?.ReasonCode, errorResponse?.ReasonDesc },
                            ["Response"] = new[] { errorResponse?.ResponseCode, errorResponse?.ResponseDesc },
                            ["Success"] = new[] { errorResponse?.SuccessCode, errorResponse?.SuccessDesc }
                        };
                        var validationError = new ValidationProblemDetails(errors);
                        return ValidationProblem(validationError);
                    }
                    catch (JsonSerializationException)
                    {
                        return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
                        {
                            ["Error"] = new[] { response.Error?.Content ?? "An unknown error occurred" }
                        }));
                    }

                case HttpStatusCode.Unauthorized:
                    _logger.LogWarning("Unauthorized response in Void Vendor Payment for Transaction {TransactionId} by {DisplayName}",
                        request.TransactionId, user);
                    return Unauthorized();

                case HttpStatusCode.NotFound:
                    _logger.LogWarning("Not Found response in Void Vendor Payment for Transaction {TransactionId} by {DisplayName}",
                        request.TransactionId, user);
                    return NotFound();

                default:
                    _logger.LogError(response.Error,
                        "Unknown response from Void Vendor Payment for Transaction {TransactionId} by {DisplayName}", request.TransactionId, user);
                    return StatusCode(500);
            }
        }
    }

    /// <summary>
    /// Voids the payment
    /// </summary>
    /// <param name="request">Entity containing Requester Name, TransactionId and Reason Code for a void payment request</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>OK if successful</returns>
    [HttpPost("void-payment-with-reason-code")]
    [ProducesResponseType(typeof(UnloadVendorPaymentResponse), Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), Status400BadRequest)]
    [ProducesResponseType(typeof(void), Status404NotFound)]
    [Authorize(Roles = "B2C Update Payment")]
    public async Task<ActionResult> VoidPaymentWithReasonCode([FromBody] VendorVoidPaymentWithReasonCodeRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var user = _userInfo.DisplayName;
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["DisplayName"] = user,
            ["TransactionId"] = request.TransactionId
        }))
        {
            var transactionInfo = await _mediator.Send(new TransactionInfo.Query(request.TransactionId), cancellationToken)
                .ConfigureAwait(false);

            if (transactionInfo.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Not Found response in Transaction Info for Transaction {TransactionId} by {DisplayName}",
                    request.TransactionId, user);
                return NotFound();
            }

            if (!transactionInfo.IsSuccessStatusCode || transactionInfo.Content == null)
            {
                _logger.LogError(transactionInfo.Error,
                    "Unknown response from Transaction Info for Transaction {TransactionId} by {DisplayName}", request.TransactionId, user);
                return StatusCode(500);
            }

            if (!_userInfo.CanAccess(transactionInfo.Content.ClientCode, transactionInfo.Content.BillingEntityCode))
            {
                _logger.LogWarning(
                    "Attempt to Void payment denied due to missing role allowing it in Void Payment for Transaction {TransactionId} by {DisplayName}",
                    request.TransactionId, user);
                return Unauthorized();
            }

            var response = await _mediator.Send(new UnloadVendorPayment.Query(user, request.TransactionId, request.ReasonCode), cancellationToken)
                .ConfigureAwait(false);

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    _logger.LogInformation("Vendor Payment Voided Successfully for Transaction {TransactionId} by {DisplayName}",
                        request.TransactionId, user);
                    return Ok(response.Content);

                case HttpStatusCode.BadRequest:
                    _logger.LogWarning("Bad Request response in Void Vendor Payment for Transaction {TransactionId} by {DisplayName}",
                        request.TransactionId, user);

                    try
                    {
                        var errorResponse = response.Content ??
                                            JsonConvert.DeserializeObject<UnloadVendorPaymentResponse>(
                                                response.Error?.Content ?? "");

                        var errors = new Dictionary<string, string[]>
                        {
                            ["Reason"] = [errorResponse?.ReasonCode, errorResponse?.ReasonDesc],
                            ["Response"] = [errorResponse?.ResponseCode, errorResponse?.ResponseDesc],
                            ["Success"] = [errorResponse?.SuccessCode, errorResponse?.SuccessDesc]
                        };
                        var validationError = new ValidationProblemDetails(errors);
                        return ValidationProblem(validationError);
                    }
                    catch (JsonSerializationException)
                    {
                        return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
                        {
                            ["Error"] = [response.Error?.Content ?? "An unknown error occurred"]
                        }));
                    }

                case HttpStatusCode.Unauthorized:
                    _logger.LogWarning("Unauthorized response in Void Vendor Payment for Transaction {TransactionId} by {DisplayName}",
                        request.TransactionId, user);
                    return Unauthorized();

                case HttpStatusCode.NotFound:
                    _logger.LogWarning("Not Found response in Void Vendor Payment for Transaction {TransactionId} by {DisplayName}",
                        request.TransactionId, user);
                    return NotFound();

                default:
                    _logger.LogError(response.Error,
                        "Unknown response from Void Vendor Payment for Transaction {TransactionId} by {DisplayName}", request.TransactionId, user);
                    return StatusCode(500);
            }
        }
    }

    /// <summary>
    /// Gets the documents associated with the transaction.
    /// </summary>
    /// <param name="transactionId">The ID of the transaction to retrieve documents for.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the work.</param>
    /// <response code="200">If documents are retrieved successfully for the transaction.</response>
    /// <response code="400">If the request contains invalid data.</response>
    /// <response code="401">If the user cannot be authorized.</response>
    /// <response code="403">If the user does not have the required role.</response>
    /// <response code="404">If the transactionId cannot be found.</response>
    [HttpGet("documents/{transactionId}")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status400BadRequest)]
    [ProducesResponseType(Status401Unauthorized)]
    [ProducesResponseType(Status403Forbidden)]
    [ProducesResponseType(Status404NotFound)]
    [Authorize(Roles = "B2C Get Documents")]
    public async Task<ActionResult<List<FilteredDocumentDto>>> GetDocumentList(
        long transactionId,
        CancellationToken cancellationToken = default)
    {
        var user = _userInfo.DisplayName;

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["DisplayName"] = user,
            ["TransactionId"] = transactionId
        });

        try
        {
            var documents = await _mediator
                .Send(new GetFilteredDocumentsByTransactionIdQuery(transactionId), cancellationToken)
                .ConfigureAwait(false);

            if (documents == null)
            {
                return NotFound();
            }

            var documentDtos = _mapper.Map<List<FilteredDocumentDto>>(documents);

            return Ok(documentDtos);
        }
        catch (NotAuthorizedException ex)
        {
            _logger.LogWarning(ex, "Document list - Unauthorized client tried to get list of documents for Transaction [{TransactionId}] by {DisplayName}.", transactionId, user);
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Document list - An error occurred while retrieving documents for Transaction [{TransactionId}] by {DisplayName}.", transactionId, user);
            return StatusCode(500, "An error occurred while processing your request.");
        }
    }

    /// <summary>
    /// Gets the document with the given transaction id and document id.
    /// </summary>
    /// <param name="transactionId">The ID of the transaction the document is associated with.</param>
    /// <param name="documentId">The ID of the document to retrieve.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the work.</param>
    /// <response code="200">If documents are retrieved successfully for the transaction.</response>
    /// <response code="400">If the request contains invalid data.</response>
    /// <response code="401">If the user cannot be authorized.</response>
    /// <response code="403">If the user does not have the required role.</response>
    /// <response code="404">If the transaction id or document id cannot be found.</response>
    [HttpGet("documents/{transactionId}/{documentId}/download")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status400BadRequest)]
    [ProducesResponseType(Status401Unauthorized)]
    [ProducesResponseType(Status403Forbidden)]
    [ProducesResponseType(Status404NotFound)]
    [Produces("application/octet-stream", "application/json", "text/plain")]
    [Authorize(Roles = "B2C Get Documents")]
    public async Task<ActionResult> GetDocument(
        long transactionId,
        long documentId,
        CancellationToken cancellationToken = default)
    {
        var user = _userInfo.DisplayName;

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["DisplayName"] = user,
            ["TransactionId"] = transactionId,
            ["DocumentId"] = documentId
        });

        try
        {
            var document = await _mediator
                .Send(new DownloadDocumentByIdQuery(transactionId, documentId), cancellationToken)
                .ConfigureAwait(false);

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
            _logger.LogWarning(ex, "Document download - Unauthorized client tried to download document [{TransactionId} - {DocumentId}] by {DisplayName}.", transactionId, documentId, user);
            return NotFound();
        }
        catch (DocumentNotInArchiveException ex)
        {
            _logger.LogWarning(ex, "Document download - Document {DocumentId} does not exist for Transaction {TransactionId} by {DisplayName}.", documentId, transactionId, user);
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Document download - An error occurred while downloading document {DocumentId} for Transaction [{TransactionId}] by {DisplayName}.", documentId, transactionId, user);
            return StatusCode(500, "An error occurred while processing your request.");
        }
    }

    [HttpPost("add-bank-account")]
    [ProducesResponseType(typeof(AddBankAccountSuccessResponse), Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), Status404NotFound)]
    [Authorize(Roles = "B2C Add Bank Account")]
    public async Task<ActionResult> AddBankAccount([FromBody] VendorAddBankAccountDto request,
        CancellationToken cancellationToken = default)
    {
        string errorTitle;
        string errorMessage;
        string user = _userInfo.DisplayName;
        string logPayerCode = request.PayerCode?.Replace(Environment.NewLine, string.Empty) ?? "N/A";
        string logSubPayerCode = request.SubPayerCode?.Replace(Environment.NewLine, string.Empty) ?? "N/A";
        string logClientMemberId = request.ClientMemberId?.Replace(Environment.NewLine, string.Empty) ?? "N/A";
        string logZetaMemberId = request.ZetaMemberId?.Replace(Environment.NewLine, string.Empty) ?? "N/A";
        string logAction = "AddBankAccount";

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["DisplayName"] = user,
            ["Payer"] = logPayerCode,
            ["SubPayer"] = logSubPayerCode,
            ["Action"] = logAction
        }))
        {
            try
            {
                var otb = await _mediator.Send(new OriginClientQuery(request.PayerCode), cancellationToken).ConfigureAwait(false);
                if (otb == null || otb.Origin == null)
                {
                    errorMessage = "Invalid Payer Code";
                    _logger.LogWarning("{Action} - {ErrorMessage} for Payer {ClientCode} ({BillingEntityCode}) by {DisplayName}", logAction, errorMessage, logPayerCode, logSubPayerCode, user);
                    var details = CreateValidationDetails(nameof(request.PayerCode), errorMessage);
                    return ValidationProblem(details);
                }

                var clientInfo = await _mediator.Send(new BillingEntityQuery(request.PayerCode, request.SubPayerCode), cancellationToken).ConfigureAwait(false);
                if (clientInfo == null)
                {
                    errorMessage = "Invalid Sub-Payer Code";
                    _logger.LogWarning("{Action} - {ErrorMessage} for Payer {ClientCode} ({BillingEntityCode}) by {DisplayName}", logAction, errorMessage, logPayerCode, logSubPayerCode, user);
                    var details = CreateValidationDetails(nameof(request.SubPayerCode), errorMessage);
                    return ValidationProblem(details);
                }

                var logOriginCode = otb?.Origin?.OriginCode?.Replace(Environment.NewLine, string.Empty) ?? "N/A";
                _logger.LogInformation("{Action} - Sending Request to RestApi with params Origin: {Origin}, ClientCode: {ClientCode}, BillingEntityCode: {BillingEntityCode}, ClientMemberId: {ClientMemberId}, ZetaMemberId: {ZetaMemberId}",
                    logAction, logOriginCode, logPayerCode, logSubPayerCode, logClientMemberId, logZetaMemberId);

                var response = await _mediator.Send(new AddBankAccount.Command(
                    otb.Origin.OriginCode, request.PayerCode, request.SubPayerCode,
                    request.ClientMemberId, request.ZetaMemberId,
                    request.BankRoutingNumber, request.BankAccountNumber), cancellationToken)
                    .ConfigureAwait(false);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return Ok(new AddBankAccountSuccessResponse
                    {
                        OptumMemberId = response.Content.vPayMemberId
                    });
                }

                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    errorTitle = "Invalid Input";
                    errorMessage = "Missing required fields or invalid data";
                    _logger.LogWarning("{Action} - {ErrorMessage} adding bank account for Member {ClientMemberId} by {DisplayName}", logAction, errorMessage, logClientMemberId, user);
                    return Problem(detail: errorMessage, statusCode: 400, title: errorTitle, type: "https://httpstatuses.io/400");
                }

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    errorTitle = "Unauthorized Request";
                    errorMessage = "User not allowed to perform this action";
                    _logger.LogWarning("{Action} - {ErrorMessage} adding bank account for Member {ClientMemberId} by {DisplayName}", logAction, errorMessage, logClientMemberId, user);
                    return Problem(detail: errorMessage, statusCode: 401, title: errorTitle, type: "https://httpstatuses.io/401");
                }

                errorTitle = "Unexpected Error";
                errorMessage = "An unexpected error occurred";
                _logger.LogWarning("{Action} - {ErrorMessage} adding bank account for Member {ClientMemberId} by {DisplayName}", logAction, errorMessage, logClientMemberId, user);
                return Problem(detail: errorMessage, statusCode: 400, title: errorTitle, type: "https://httpstatuses.io/400");
            }
            catch (NotAuthorizedException ex)
            {
                errorTitle = "Forbidden Request";
                errorMessage = "User not allowed to perform this action";
                _logger.LogError(ex, "{Action} - {ErrorMessage} adding bank account for Member {ClientMemberId} by {DisplayName}", logAction, errorMessage, logClientMemberId, user);
                return Problem(detail: errorMessage, statusCode: 403, title: errorTitle, type: "https://httpstatuses.io/403");
            }
            catch (Exception ex)
            {
                errorTitle = "Unexpected Error";
                errorMessage = "An unexpected error occurred";
                _logger.LogError(ex, "{Action} - {ErrorMessage} adding bank account for Member {ClientMemberId} by {DisplayName}", logAction, errorMessage, logClientMemberId, user);
                return Problem(detail: errorMessage, statusCode: 400, title: errorTitle, type: "https://httpstatuses.io/400");
            }
        }
    }

    /// <summary>
    /// Expires the Bank Account for the given client member id,origin, payer code, sub payer code and zeta member id.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>OK if successful</returns>
    [HttpPost("expire-bank-account")]
    [ProducesResponseType(typeof(ExpireBankAccountSuccessResponse), Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), Status404NotFound)]
    [Authorize(Roles = "B2C Expire Bank Account")]
    public async Task<ActionResult> ExpireBankAccount([FromBody] VendorExpireBankAccountDto request,
            CancellationToken cancellationToken = default)
    {
        string errorTitle;
        string errorMessage;
        string user = _userInfo.DisplayName;
        string logPayerCode = request.PayerCode?.Replace(Environment.NewLine, string.Empty) ?? "N/A";
        string logSubPayerCode = request.SubPayerCode?.Replace(Environment.NewLine, string.Empty) ?? "N/A";
        string logClientMemberId = request.ClientMemberId?.Replace(Environment.NewLine, string.Empty) ?? "N/A";
        string logZetaMemberId = request.ZetaMemberId?.Replace(Environment.NewLine, string.Empty) ?? "N/A";
        string logAction = "ExpireBankAccount";


        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["DisplayName"] = user,
            ["Payer"] = logPayerCode,
            ["SubPayer"] = logSubPayerCode,
            ["Action"] = logAction
        }))
        {
            try
            {
                var otb = await _mediator.Send(new OriginClientQuery(request.PayerCode), cancellationToken).ConfigureAwait(false);
                if (otb == null || otb.Origin == null)
                {
                    errorMessage = "Invalid Payer Code";
                    _logger.LogWarning("{Action} - {ErrorMessage} for Payer {ClientCode} ({BillingEntityCode}) by {DisplayName}", logAction, errorMessage, logPayerCode, logSubPayerCode, user);
                    var details = CreateValidationDetails(nameof(request.PayerCode), errorMessage);
                    return ValidationProblem(details);
                }

                var clientInfo = await _mediator.Send(new BillingEntityQuery(request.PayerCode, request.SubPayerCode), cancellationToken).ConfigureAwait(false);
                if (clientInfo == null)
                {
                    errorMessage = "Invalid Sub-Payer Code";
                    _logger.LogWarning("{Action} - {ErrorMessage} for Payer {ClientCode} ({BillingEntityCode}) by {DisplayName}", logAction, errorMessage, logPayerCode, logSubPayerCode, user);
                    var details = CreateValidationDetails(nameof(request.SubPayerCode), errorMessage);
                    return ValidationProblem(details);
                }

                if (!_userInfo.CanAccess(request.PayerCode, request.SubPayerCode))
                {
                    errorTitle = "Unauthorized Request";
                    errorMessage = "User not configured for provided Payer Code and/or Sub-Payer Code";
                    _logger.LogWarning("{Action} - {ErrorMessage} expiring bank account for Member {ClientMemberId} by {DisplayName}", logAction, errorMessage, logClientMemberId, user);
                    return Problem(detail: errorMessage, statusCode: 401, title: errorTitle, type: "https://httpstatuses.io/401");
                }

                var logOriginCode = otb?.Origin?.OriginCode?.Replace(Environment.NewLine, string.Empty) ?? "N/A";
                _logger.LogInformation("{Action} - Sending Request to RestApi with params Origin: {Origin}, ClientCode: {ClientCode}, BillingEntityCode: {BillingEntityCode}, ClientMemberId: {ClientMemberId}, ZetaMemberId: {ZetaMemberId}",
                    logAction, logOriginCode, logPayerCode, logSubPayerCode, logClientMemberId, logZetaMemberId);

                var response = await _mediator.Send(new ExpireBankAccount.ExpireBankAccountCommand(
                    otb.Origin.OriginCode, request.PayerCode, request.SubPayerCode,
                    request.ClientMemberId, request.ZetaMemberId), cancellationToken)
                    .ConfigureAwait(false);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return Ok(new ExpireBankAccountSuccessResponse
                    {
                        OptumMemberId = response.Content.optumMemberId
                    });
                }

                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    errorTitle = "Invalid Input";
                    errorMessage = "Missing required fields or invalid data";
                    _logger.LogWarning("{Action} - {ErrorMessage} expiring bank account for Member {ClientMemberId} by {DisplayName}", logAction, errorMessage, logClientMemberId, user);
                    return Problem(detail: errorMessage, statusCode: 400, title: errorTitle, type: "https://httpstatuses.io/400");
                }

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    errorTitle = "Unauthorized Request";
                    errorMessage = "User not allowed to perform this action";
                    _logger.LogWarning("{Action} - {ErrorMessage} expiring bank account for Member {ClientMemberId} by {DisplayName}", logAction, errorMessage, logClientMemberId, user);
                    return Problem(detail: errorMessage, statusCode: 401, title: errorTitle, type: "https://httpstatuses.io/401");
                }

                errorTitle = "Unexpected Error";
                errorMessage = "An unexpected error occurred";
                _logger.LogWarning("{Action} - {ErrorMessage} expiring bank account for Member {ClientMemberId} by {DisplayName}", logAction, errorMessage, logClientMemberId, user);
                return Problem(detail: errorMessage, statusCode: 400, title: errorTitle, type: "https://httpstatuses.io/400");
            }
            catch (NotAuthorizedException ex)
            {
                errorTitle = "Forbidden Request";
                errorMessage = "User not allowed to perform this action";
                _logger.LogError(ex, "{Action} - {ErrorMessage} expiring bank account for Member {ClientMemberId} by {DisplayName}", logAction, errorMessage, logClientMemberId, user);
                return Problem(detail: errorMessage, statusCode: 403, title: errorTitle, type: "https://httpstatuses.io/403");
            }
            catch (Exception ex)
            {
                errorTitle = "Unexpected Error";
                errorMessage = "An unexpected error occurred";
                _logger.LogError(ex, "{Action} - {ErrorMessage} expiring bank account for Member {ClientMemberId} by {DisplayName}", logAction, errorMessage, logClientMemberId, user);
                return Problem(detail: errorMessage, statusCode: 400, title: errorTitle, type: "https://httpstatuses.io/400");
            }
        }
    }

    /// <summary>
    /// Receives the payment decision information from our vendor partner.
    /// </summary>
    /// <param name="request">Entity containing the payment decision info for a given payment from our vendor partner.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>OK if successful</returns>
    [HttpPost("receive-payment-info")]
    [ProducesResponseType(typeof(VendorPaymentResponseDto), Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), Status409Conflict)]
    [ProducesResponseType(typeof(void), Status404NotFound)]
    [Authorize(Roles = "B2C Post Payment Decision")]
    public async Task<ActionResult> ReceivePaymentDecision(
        [FromBody] VendorPaymentDecisionDto request,
        CancellationToken cancellationToken = default)
    {
        string errorTitle;
        string errorMessage;

        var user = _userInfo.DisplayName;
        var logDisplayName = SanitizeForLog(user);
        var logAction = nameof(ReceivePaymentDecision);

        var logPayerCode = SanitizeForLog(request?.PayerCode);
        var logSubPayerCode = SanitizeForLog(request?.SubpayerCode);
        var logPreTransId = SanitizeForLog(request?.PreTransId);
        var logDecisionedModality = SanitizeForLog(request?.DecisionedModality);
        var logClaimRejectionReason = SanitizeForLog(request?.ClaimRejectionReason);
        var logZetaTransactionId = SanitizeForLog(request?.ZetaTransactionId);
        var logNoSelectionExpiredIndicator = (request?.NoSelectionExpiredIndicator ?? false)
            .ToString(CultureInfo.InvariantCulture);

        var logMemberIds = request?.MemberIDs?
            .Select((member, index) => new Dictionary<string, object?>
            {
                ["Index"] = index,
                ["ClientMemberId"] = SanitizeForLog(member?.ClientMemberId),
                ["ZetaMemberId"] = SanitizeForLog(member?.ZetaMemberId),
                ["OptumMemberId"] = SanitizeForLog(member?.OptumMemberId),
                ["PartyId"] = SanitizeForLog(member?.PartyId)
            })
            .ToList() ?? [];

        var logMemberIdsJson = JsonConvert.SerializeObject(logMemberIds);

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["DisplayName"] = logDisplayName,
            ["ClientCode"] = logPayerCode,
            ["BillingEntityCode"] = logSubPayerCode,
            ["PreTransactionId"] = logPreTransId,
            ["Action"] = logAction
        }))
        {
            try
            {
                _logger.LogInformation("Received request to {Action} for PreTransaction {PreTransactionId} " +
                 "by {DisplayName} with Payer Code = {ClientCode} and Sub-Payer Code = {BillingEntityCode}" +
                 "Decision Modality : {DecisionModality} Claim Rejection Reason : {ClaimRejectionReason}" +
                  "Zeta Transaction Id : {ZetaTransactionId} No Selection ExpiredIndicator : {NoSelectionExpiredIndicator} MemberIds : {MemberIds}",
                  logAction, logPreTransId, user, logPayerCode, logSubPayerCode, logDecisionedModality,
                  logClaimRejectionReason, logZetaTransactionId, logNoSelectionExpiredIndicator, logMemberIdsJson);
                var clientInfo = await _mediator.Send(new BillingEntityQuery(request.PayerCode, request.SubpayerCode), cancellationToken).ConfigureAwait(false);
                if (clientInfo == null)
                {
                    _logger.LogWarning("Client or BE not found for PreTransaction {PreTransactionId} by {DisplayName} for ClientCode = {ClientCode}, Billing Entity = {BillingEntityCode}", logPreTransId, user, logPayerCode, logSubPayerCode);
                    errorMessage = "Payer Code and Sub-Payer Code must be valid";
                    var details = CreateValidationDetails(new Dictionary<string, string>
                    {
                        [nameof(request.PayerCode)] = errorMessage,
                        [nameof(request.SubpayerCode)] = errorMessage
                    });
                    return ValidationProblem(details);
                }

                if (!_userInfo.CanAccess(request.PayerCode, request.SubpayerCode))
                {
                    errorTitle = "Unauthorized Request";
                    errorMessage = "User not configured for provided Payer Code and/or Sub-Payer Code";
                    _logger.LogWarning("{ErrorMessage} processing decision for PreTransaction {PreTransactionId} by {DisplayName}", errorMessage, logPreTransId, user);
                    return Problem(detail: errorMessage, statusCode: 401, title: errorTitle, type: "https://httpstatuses.io/401");
                }

                // Call Member Claims API to forward the payment decision received from the vendor partner.
                _logger.LogInformation("Calling Member Claims API to process payment decision for PreTransaction {PreTransactionId} by {DisplayName} with Payer Code = {ClientCode} and Sub-Payer Code = {BillingEntityCode}",
    logPreTransId, logDisplayName, logPayerCode, logSubPayerCode);
                var paymentInfo = request.FromDto();
                var response = await _mediator
                    .Send(new ReceivePaymentDecision.Command(paymentInfo), cancellationToken)
                    .ConfigureAwait(false);

                if (response == null)
                {
                    errorTitle = "Invalid Input";
                    errorMessage = "Missing required fields or invalid data: Member Claims API null response";

                    _logger.LogError("{ErrorMessage} processing decision for PreTransaction {PreTransactionId} by {DisplayName}", errorMessage, logPreTransId, user);
                    return Problem(detail: errorMessage, statusCode: 400, title: errorTitle, type: "https://httpstatuses.io/400");
                }
                else if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Member Claims API - Successful response processing decision for PreTransaction {PreTransactionId} by {DisplayName}", logPreTransId, user);
                    return Ok(new VendorPaymentResponse().Ok());
                }
                else if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    errorTitle = "Not Found";
                    errorMessage = $"Member Claims API - One or more required resources were not found: {response.Content.Detail ?? "N/A"}";
                    _logger.LogWarning("{ErrorMessage} processing decision for PreTransaction {PreTransactionId} by {DisplayName}", errorMessage, logPreTransId, user);
                    return Problem(detail: errorMessage, statusCode: 404, title: errorTitle, type: "https://httpstatuses.io/404");
                }
                else if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    errorTitle = "Request Conflict";
                    errorMessage = $"Member Claims API - One or more data conflicts occurred: {response.Content.Detail ?? "N/A"}";
                    _logger.LogError("{ErrorMessage} processing decision for PreTransaction {PreTransactionId} by {DisplayName}", errorMessage, logPreTransId, user);
                    return Problem(detail: errorMessage, statusCode: 409, title: errorTitle, type: "https://httpstatuses.io/409");
                }
                else if (response.Content?.Errors?.Count > 0)
                {
                    errorMessage = $"Member Claims API - One or more validation errors occurred: {response.Content.Detail ?? "N/A"}";
                    var details = CreateValidationDetails(response.Content.Errors);
                    return ValidationProblem(details);
                }
                else // Other non-success status code with no validation errors or specific handling implemented i.e. Internal Server Error
                {
                    errorTitle = "Unexpected Error";
                    errorMessage = $"Member Claims API - Unsuccessful response: {response.Content.Detail ?? "N/A"}";
                    _logger.LogError("{ErrorMessage} processing decision for PreTransaction {PreTransactionId} by {DisplayName}", errorMessage, logPreTransId, user);
                    return Problem(detail: errorMessage, statusCode: 409, title: errorTitle, type: "https://httpstatuses.io/409");
                }
            }
            catch (NotAuthorizedException ex)
            {
                errorTitle = "Forbidden Request";
                errorMessage = "User not allowed to perform this action";
                _logger.LogError(ex, "{Action} - {ErrorMessage} getting payment detail for PreTransaction {PreTransactionId} by {DisplayName}", logAction, errorMessage, logPreTransId, user);
                return Problem(detail: errorMessage, statusCode: 403, title: errorTitle, type: "https://httpstatuses.io/403");
            }
            catch (Exception ex)
            {
                errorTitle = "Unexpected Error";
                errorMessage = ex.Message ?? "An unexpected error occurred";
                _logger.LogError(ex, "{Action} - {ErrorMessage} getting payment detail for PreTransaction {PreTransactionId} by {DisplayName}", logAction, errorMessage, logPreTransId, user);
                return Problem(detail: errorMessage, statusCode: 400, title: errorTitle, type: "https://httpstatuses.io/400");
            }
        }
    }

    /// <summary>
    /// Creates a ValidationProblemDetails object from a dictionary of property names and error messages, and adds the error messages to the ModelState.
    /// </summary>
    /// <param name="errorMessages">The key/value pair of error messages where the key represents the property name and the value the error message.</param>
    /// <returns>The <see cref="ValidationProblemDetails"/> object.</returns>
    private static ValidationProblemDetails CreateValidationDetails(IDictionary<string, string[]> errorMessages)
    {
        return new ValidationProblemDetails(errorMessages)
        {
            Title = "One or more validation errors occurred",
            Detail = null,
            Type = "https://httpstatuses.io/400",
            Status = 400
        };
    }

    /// <summary>
    /// Creates a ValidationProblemDetails object for a single property and error message, and adds the error message to the ModelState.
    /// </summary>
    /// <param name="propertyName">The name of the property.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>The <see cref="ValidationProblemDetails"/> object.</returns>
    private static ValidationProblemDetails CreateValidationDetails(string propertyName, string errorMessage)
    {
        var errorMessages = new Dictionary<string, string[]>
        {
            [propertyName] = [errorMessage]
        };
        return CreateValidationDetails(errorMessages);
    }

    /// <summary>
    /// Creates a ValidationProblemDetails object from a dictionary of property names and error messages, and adds the error messages to the ModelState.
    /// </summary>
    /// <param name="errorMessages">The key/value pair of error messages where the key represents the property name and the value the error message.</param>
    /// <returns>The <see cref="ValidationProblemDetails"/> object.</returns>
    private static ValidationProblemDetails CreateValidationDetails(IDictionary<string, string> errorMessages)
    {
        return CreateValidationDetails(errorMessages.ToDictionary(kv => kv.Key, kv => new[] { kv.Value }));
    }
    private static string SanitizeForLog(string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }
        var normalized = input.Replace("\r", string.Empty).Replace("\n", string.Empty);
        return new string(normalized.Where(c => !char.IsControl(c)).ToArray());
    }

}
