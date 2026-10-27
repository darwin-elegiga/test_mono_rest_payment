using System.ComponentModel.DataAnnotations;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using VPay.Extensions.Exceptions;
using VPay.Gateway.Api.Models;
using VPay.Gateway.Application;
using VPay.Gateway.Common.Documents;
using VPay.Gateway.Common.Kubera;
using VPay.Gateway.Common.Kubera.Models;
using VPay.Gateway.Common.Loaf;
using VPay.Gateway.Common.RestPayments;
using VPay.Gateway.Data.Sql.Transactions;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace VPay.Gateway.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public partial class PaymentsController : ControllerBase
{
    private readonly IUserInfo _userInfo;
    private readonly IMediator _mediator;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(IUserInfo userInfo, IMediator mediator, ILogger<PaymentsController> logger)
    {
        _userInfo = userInfo ?? throw new ArgumentNullException(nameof(userInfo));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Voids the payment
    /// </summary>
    /// <param name="transactionId">The vpay transaction identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>OK if successful</returns>
    [HttpDelete("void")]
    [ProducesResponseType(typeof(VoidPayment.Response), Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), Status400BadRequest)]
    [ProducesResponseType(typeof(void), Status404NotFound)]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Void Payment")]
    public async Task<ActionResult> VoidPayment([Required][Range(1, long.MaxValue)] long transactionId,
        CancellationToken cancellationToken = default)
    {
        var user = _userInfo.DisplayName;
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["DisplayName"] = user,
            ["TransactionId"] = transactionId
        }))
        {
            var transactionInfo = await _mediator.Send(new TransactionInfo.Query(transactionId), cancellationToken)
                .ConfigureAwait(false);

            if (transactionInfo.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Not Found response in Transaction Info for {TransactionId} by {DisplayName}",
                    transactionId, user);
                return NotFound();
            }

            if (!transactionInfo.IsSuccessStatusCode || transactionInfo.Content == null)
            {
                _logger.LogError(transactionInfo.Error,
                    "Unknown response from Transaction Info for {TransactionId} by {DisplayName}", transactionId, user);
                return StatusCode(500);
            }

            if (!_userInfo.CanAccess(transactionInfo.Content.ClientCode, transactionInfo.Content.BillingEntityCode))
            {
                _logger.LogWarning(
                    "Attempt to Void payment denied due to missing role allowing it in Void Payment for {TransactionId} by {DisplayName}",
                    transactionId, user);
                return Unauthorized();
            }

            var response = await _mediator.Send(new VoidPayment.Query(user, transactionId), cancellationToken)
                .ConfigureAwait(false);

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    _logger.LogInformation("Payment Voided Successfully for {TransactionId} by {DisplayName}",
                        transactionId, user);
                    return Ok(response.Content);

                case HttpStatusCode.BadRequest:
                    _logger.LogWarning("Bad Request response in Void Payment for {TransactionId} by {DisplayName}",
                        transactionId, user);

                    try
                    {
                        var errorResponse = response.Content ??
                                            JsonConvert.DeserializeObject<VoidPayment.Response>(
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
                    _logger.LogWarning("Unauthorized response in Void Payment for {TransactionId} by {DisplayName}",
                        transactionId, user);
                    return Unauthorized();

                case HttpStatusCode.NotFound:
                    _logger.LogWarning("Not Found response in Void Payment for {TransactionId} by {DisplayName}",
                        transactionId, user);
                    return NotFound();

                default:
                    _logger.LogError(response.Error,
                        "Unknown response from Void Payment for {TransactionId} by {DisplayName}", transactionId, user);
                    return StatusCode(500);
            }
        }
    }

    /// <summary>
    /// Gets the Card Details for a Payment
    /// </summary>
    /// <param name="payerId">Payer Id</param>
    /// <param name="subPayerId">Sub Payer Id</param>
    /// <param name="paymentId">The payment identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>OK if successful</returns>
    [HttpGet("card-details")]
    [ProducesResponseType(typeof(GetCardDetails.Response), Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), Status400BadRequest)]
    [ProducesResponseType(typeof(void), Status404NotFound)]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Card Details")]
    public async Task<ActionResult> GetCardDetails([Required(AllowEmptyStrings = false)] string payerId,
        [Required(AllowEmptyStrings = false)] string subPayerId, [Required(AllowEmptyStrings = false)] string paymentId,
        CancellationToken cancellationToken = default)
    {
        //PayerId is not really needed but we decided to require it to standardize like we do for Billing Entity and Client Code
        var user = _userInfo.DisplayName;
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["DisplayName"] = user,
            ["PayerId"] = payerId,
            ["SubPayerId"] = subPayerId,
            ["PaymentId"] = paymentId
        }))
        {
            PayerSubPayerAndPaymentNumberToTransactionId.Response paymentInfoResponse;
            try
            {
                paymentInfoResponse = await _mediator
                    .Send(new PayerSubPayerAndPaymentNumberToTransactionId.Query(payerId, subPayerId, paymentId), cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (ItemNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Attempt to get PaymentNumberToTransactionId Failed with unexpected error for {SubPayerId} - {PaymentId}", subPayerId.Replace(Environment.NewLine, string.Empty), paymentId.Replace(Environment.NewLine, string.Empty));
                return StatusCode(500);
            }

            if (!_userInfo.CanAccess(paymentInfoResponse.ClientCode, paymentInfoResponse.BillingEntityCode))
            {
                _logger.LogWarning("Attempt to get card details denied due to missing role allowing it in Card Details for {ClientCode} - {BillingEntityCode} for Payment: {PaymentId} by {DisplayName}",
                    paymentInfoResponse.ClientCode, paymentInfoResponse.BillingEntityCode, paymentId.Replace(Environment.NewLine, string.Empty), user);
                return Unauthorized();
            }

            var cardDetails = await _mediator
                .Send(new GetCardDetails.Query(paymentId, paymentInfoResponse.ClientCode, paymentInfoResponse.BillingEntityCode, paymentInfoResponse.TransactionId), cancellationToken)
                .ConfigureAwait(false);

            if (cardDetails.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning(
                    "Not Found response in GetCardDetails Info for ClientCode: {ClientCode} - BillingEntityCode: {BillingEntityCode} - TransactionId: {TransactionId}",
                    paymentInfoResponse.ClientCode, paymentInfoResponse.BillingEntityCode,
                    paymentInfoResponse.TransactionId);

                return NotFound();
            }

            if (!cardDetails.IsSuccessStatusCode || cardDetails.Content == null)
            {
                _logger.LogError(cardDetails.Error,
                    "Unknown response from Transaction Info for {TransactionId}", paymentInfoResponse.TransactionId);
                return StatusCode(500);
            }

            _logger.LogInformation("Card Details for {TransactionId} returned successfully", paymentInfoResponse.TransactionId);

            // User has access to the CC/BE so send back the card details
            return Ok(cardDetails.Content);
        }
    }

    /// <summary>
    /// Gets the Card Details for Vpay Transaction Id
    /// </summary>
    /// <param name="transactionId">Transaction Id</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>OK if successful</returns>
    [HttpGet("card-details/transactionId")]
    [ProducesResponseType(typeof(GetCardDetails.Response), Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), Status400BadRequest)]
    [ProducesResponseType(typeof(void), Status404NotFound)]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Card Details")]
    public async Task<ActionResult> GetCardDetailsForTransactionId([Required(AllowEmptyStrings = false)] long transactionId, CancellationToken cancellationToken = default)
    {
        var user = _userInfo.DisplayName;
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["DisplayName"] = user,
            ["TransactionId"] = transactionId
        }))
        {
            Common.Documents.Models.TransactionPayment paymentDetails;
            try
            {
                //paymentDetails = await _mediator
                //    .Send(new GetPaymentByTransactionIdQuery(transactionId), cancellationToken)
                //    .ConfigureAwait(false);

                paymentDetails = await _mediator
                    .Send(new GetDocumentPaymentDetailByTransactionIDQuery(transactionId), cancellationToken)
                    .ConfigureAwait(false);
                if (paymentDetails is null)
                {
                    _logger.LogInformation("Payment Id not found for {TransactionId} ", transactionId);
                    return NotFound();
                }
            }
            catch (ItemNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Attempt to get Payment Id failed with unexpected error for {TransactionId} ", transactionId);
                return StatusCode(500);
            }

            if (!_userInfo.CanAccess(paymentDetails.ClientCode, paymentDetails.BillingEntityCode))
            {
                _logger.LogWarning("Attempt to get card details denied due to missing role allowing it in Card Details for {ClientCode} - {BillingEntityCode} for Payment: {PaymentId} by {DisplayName}",
                    paymentDetails.ClientCode, paymentDetails.BillingEntityCode, paymentDetails.PaymentId.Replace(Environment.NewLine, string.Empty), user);
                return Unauthorized();
            }

            TransactionBoarding835Details traceDetails = new() { TraceNumber = "" };
            /*
                        //Get Trace Number from TransactionId
                        try
                        {
                            traceDetails = await _mediator
                               .Send(new GetBoarding835DetailsByTransactionIdQuery(transactionId), cancellationToken)
                               .ConfigureAwait(false);

                            if (traceDetails is null)
                            {
                                _logger.LogInformation("TraceDetails not found for {TransactionId} ", transactionId);
                                return NotFound();
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Attempt to get TraceNumber failed with unexpected error for {TransactionId} ", transactionId);
                            return StatusCode(500);
                        }
            */

            var cardDetails = await _mediator
                .Send(new GetCardDetails.Query(paymentDetails.PaymentId, paymentDetails.ClientCode, paymentDetails.BillingEntityCode, transactionId), cancellationToken)
                .ConfigureAwait(false);

            if (cardDetails.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning(
                    "Not Found response in GetCardDetails Info for ClientCode: {ClientCode} - BillingEntityCode: {BillingEntityCode} - TransactionId: {TransactionId}",
                    paymentDetails.ClientCode, paymentDetails.BillingEntityCode, transactionId);

                return NotFound();
            }

            if (!cardDetails.IsSuccessStatusCode || cardDetails.Content == null)
            {
                _logger.LogError(cardDetails.Error,
                    "Unknown response from Transaction Info for {TransactionId}", transactionId);
                return StatusCode(500);
            }

            _logger.LogInformation("Card Details for {TransactionId} returned successfully", transactionId);

            var additionalCardDetails = new CardDetailsForTransaction(cardDetails.Content.PaymentId, cardDetails.Content.TransactionId,
                                                                      cardDetails.Content.CreditCardNumber, cardDetails.Content.ExpirationDate, cardDetails.Content.CVCCode,
                                                                      cardDetails.Content.AddressLine1, cardDetails.Content.AddressLine2, cardDetails.Content.ZipCode, cardDetails.Content.CardBalance, traceDetails.TraceNumber);

            return Ok(additionalCardDetails);
        }
    }

    /// <summary>
    /// Given a client payment id, this endpoint will return the internal transaction id associated with that payment.
    /// </summary>
    /// <param name="clientCode">The client code the payment is associated with.</param>
    /// <param name="billingEntityCode">The billing entity the payment is associated with.</param>
    /// <param name="paymentId">The client id for the payment.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <response code="200">If the transaction is found.</response>
    /// <response code="400">If the parameters are invalid.</response>
    /// <response code="404">If the transaction cannot be found.</response>
    [HttpGet("transaction-id/{clientCode}/{billingEntityCode}/{paymentId}")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status400BadRequest)]
    [ProducesResponseType(Status404NotFound)]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<ActionResult<TransactionInformationDto>> GetTransaction(
        [Required(AllowEmptyStrings = false), MaxLength(10)] string clientCode,
        [Required(AllowEmptyStrings = false), MaxLength(20)] string billingEntityCode,
        [Required(AllowEmptyStrings = false), MaxLength(100)] string paymentId,
        CancellationToken cancellationToken = default)
    {
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["ClientCode"] = clientCode,
            ["BillingEntityCode"] = billingEntityCode,
            ["PaymentId"] = paymentId
        }))
        {
            // Get the payment information from kubera/bartok
            var allTransactions = await _mediator.Send(
                    new GetTransactionsByPaymentIdQuery(clientCode, paymentId),
                    cancellationToken)
                .ConfigureAwait(false);

            // get the most recent record based on the date it was loaded
            var transaction = (allTransactions ?? Enumerable.Empty<Transaction>())
                .Where(x => x.ClientCode == clientCode && x.BillingEntityCode == billingEntityCode)
                .MaxBy(x => x.DateLoaded);

            if (transaction is null)
            {
                Log.TransactionNotFound(_logger, clientCode, billingEntityCode, paymentId);
                return NotFound();
            }

            // if we found a transaction, make sure the account has permissions for it
            if (!_userInfo.CanAccess(transaction.ClientCode, transaction.BillingEntityCode, transaction.PaymentTypeCode))
            {
                Log.TransactionAccessDenied(_logger, clientCode, billingEntityCode, paymentId);
                return NotFound();
            }

            return Ok(
                new TransactionInformationDto
                {
                    ClientCode = transaction.ClientCode,
                    BillingEntityCode = transaction.BillingEntityCode,
                    TransactionId = transaction.TransactionId,
                    ClientPaymentId = paymentId
                });
        }
    }

    /// <summary>
    /// Reject a payment with a valid reason. This is a callback endpoint for our Network Partners.
    /// </summary>
    /// <param name="request">The entity containing the data for the payment rejection request.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <response code="200">If the rejection request was accepted.</response>
    /// <response code="400">If the entity contains invalid data.</response>
    /// <response code="401">If the user cannot be authorized.</response>
    /// <response code="403">If the user does not have the required role or permission.</response>
    [HttpPost("reject")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status400BadRequest)]
    [ProducesResponseType(Status401Unauthorized)]
    [ProducesResponseType(Status403Forbidden)]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Reject Payment")]
    public async Task<ActionResult<PaymentRejectionResultDto>> RejectNetworkPayment(
        [FromBody] PaymentRejectionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["NetworkName"] = request.NetworkPartner,
                ["TransactionId"] = request.OptumTransactionId,
                ["ReasonCode"] = request.RejectionReasonCode
            });

            // Verify request is valid.
            PaymentRejectionRequestDtoValidator validator = new();
            await validator
                .ValidateAndThrowAsync(request, cancellationToken)
                .ConfigureAwait(false);

            // Request payment rejection.
            RejectPaymentCommand command = new(
                request.NetworkPartner,
                request.OptumTransactionId.Value,
                request.RejectionReasonCode,
                request.RejectionReason,
                VerifyEnrollment: false);

            var response = await _mediator
                .Send(command, cancellationToken)
                .ConfigureAwait(false);

            PaymentRejectionResultDto result = new()
            {
                NetworkPartner = response.NetworkPartner,
                OptumTransactionId = response.TransactionId,
                ResponseMessage = response.ResponseMessage
            };

            return Ok(result);
        }
        catch (FluentValidation.ValidationException ex)
        {
            Log.RejectionValidationError(_logger, request.NetworkPartner, request.OptumTransactionId, request.RejectionReasonCode, ex.Message);
            return BadRequest(ex.Errors);
        }
        catch (ItemNotFoundException ex)
        {
            //Log.RejectionTransactionNotFound(_logger, request.NetworkPartner, request.TransactionId, request.RejectionCode);
            return BadRequest(ex.Message);
        }
        catch (NotAuthorizedException)
        {
            //Log.RejectionNotAuthenticated(_logger, ex, request.NetworkPartner, request.TransactionId, request.RejectionCode);
            return Forbid();
        }
        catch (Exception ex)
        {
            Log.RejectionOperationError(_logger, ex, request.NetworkPartner, request.OptumTransactionId, request.RejectionReasonCode);
            return BadRequest(ex.Message);
        }
    }

    private static partial class Log
    {
        [LoggerMessage(
            EventId = 0,
            Level = LogLevel.Information,
            Message = "Transactions not found for Client '{ClientCode}', Billing Entity '{BillingEntityCode}', PaymentId = {PaymentId}",
            SkipEnabledCheck = true)]
        public static partial void TransactionNotFound(ILogger logger, string clientCode, string billingEntityCode, string paymentId);

        [LoggerMessage(
            EventId = 1,
            Level = LogLevel.Warning,
            Message = "Transaction Lookup - SECURITY - Client tried to view transaction for Client '{ClientCode}', Billing Entity '{BillingEntityCode}', PaymentId = {PaymentId} when they do not have access to it.",
            SkipEnabledCheck = true)]
        public static partial void TransactionAccessDenied(ILogger logger, string clientCode, string billingEntityCode, string paymentId);

        [LoggerMessage(
            EventId = 2,
            Level = LogLevel.Information,
            Message = "Payment Rejection Request ACCEPTED [Network = '{NetworkName}', Transaction = '{TransactionId}', RejectionCode = {RejectionCode}]",
            SkipEnabledCheck = true)]
        public static partial void RejectionAccepted(ILogger logger, string networkName, long? transactionId, string rejectionCode);

        [LoggerMessage(
            EventId = 3,
            Level = LogLevel.Warning,
            Message = "Payment Rejection Request DENIED [Network = '{NetworkName}', Transaction = '{TransactionId}', RejectionCode = {RejectionCode}] : {ErrorMessage}",
            SkipEnabledCheck = true)]
        public static partial void RejectionValidationError(ILogger logger, string networkName, long? transactionId, string rejectionCode, string errorMessage);

        [LoggerMessage(
            EventId = 4,
            Level = LogLevel.Error,
            Message = "Payment Rejection Request DENIED [Network = '{NetworkName}', Transaction = '{TransactionId}', RejectionCode = {RejectionCode}] : Authentication Error",
            SkipEnabledCheck = true)]
        public static partial void RejectionNotAuthenticated(ILogger logger, Exception ex, string networkName, long? transactionId, string rejectionCode);

        [LoggerMessage(
            EventId = 5,
            Level = LogLevel.Warning,
            Message = "Payment Rejection Request DENIED [Network = '{NetworkName}', Transaction = '{TransactionId}', RejectionCode = {RejectionCode}] : Transaction ID Not Found",
            SkipEnabledCheck = true)]
        public static partial void RejectionTransactionNotFound(ILogger logger, string networkName, long? transactionId, string rejectionCode);

        [LoggerMessage(
            EventId = 6,
            Level = LogLevel.Error,
            Message = "Payment Rejection Request DENIED [Network = '{NetworkName}', Transaction = '{TransactionId}', RejectionCode = {RejectionCode}] : Operation Error",
            SkipEnabledCheck = true)]
        public static partial void RejectionOperationError(ILogger logger, Exception ex, string networkName, long? transactionId, string rejectionCode);
    }
}
