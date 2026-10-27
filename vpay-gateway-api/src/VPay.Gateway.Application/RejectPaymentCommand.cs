using System.Net;
using MediatR;
using Microsoft.Extensions.Logging;
using VPay.Extensions.Exceptions;
//using VPay.Gateway.Common.Kubera;
using VPay.Gateway.Common.Preferencing.Enums;
using VPay.Gateway.Common.RestPayments;
using VPay.Gateway.Data.Sql.Transactions;

namespace VPay.Gateway.Application;

/// <summary>
/// Request the payment rejection of a network partner payment.
/// </summary>
/// <param name="NetworkPartner">The network name of our network partner.</param>
/// <param name="TransactionId">The VPay transaction id</param>
/// <param name="RejectionCode">The rejection code: <see cref="NetworkPaymentRejectionCode"/> </param>
/// <param name="RejectionReason">The rejection reason description.</param>
/// <param name="VerifyEnrollment">Flag to check if the network is configured and active,
/// and the payor is enrolled with the network. Both checks are done via Preferencing API.
/// True to check network configuration and payor enrollment; false otherwise</param>
public sealed record RejectPaymentCommand
(
    string NetworkPartner,
    long TransactionId,
    string RejectionCode,
    string RejectionReason,
    bool VerifyEnrollment = false
) : IRequest<RejectPaymentResult>;

public sealed record RejectPaymentResult
(
    string NetworkPartner,
    long TransactionId,
    string ResponseMessage
);

public partial class RejectPaymentCommandHandler : IRequestHandler<RejectPaymentCommand, RejectPaymentResult>
{
    private readonly IMediator _mediator;
    private readonly IUserInfo _userInfo;
    private readonly ILogger<RejectPaymentCommandHandler> _logger;

    public RejectPaymentCommandHandler(
        IMediator mediator,
        IUserInfo userInfo,
        ILogger<RejectPaymentCommandHandler> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _userInfo = userInfo ?? throw new ArgumentNullException(nameof(userInfo));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<RejectPaymentResult> Handle(RejectPaymentCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var networkName = request.NetworkPartner;
        var transactionId = request.TransactionId;
        var rejectionCode = request.RejectionCode;
        var rejectionReason = request.RejectionReason;

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["NetworkName"] = networkName,
            ["TransactionId"] = transactionId,
            ["ReasonCode"] = rejectionCode
        });

        // Verify network exists (Preferencing API call).
        GetNetworkQuery networkQuery = new(networkName);
        var network = await _mediator
            .Send(networkQuery, cancellationToken)
            .ConfigureAwait(false);

        if (network == null)
        {
            Log.NetworkNotRecognized(_logger, networkName, transactionId, rejectionCode);
            throw new ItemNotFoundException("Network Partner not recognized.");
        }

        // Verify transaction exists (Kubera API call).
        //GetTransactionByIdQuery transactionQuery = new(transactionId);
        // Verify transaction exists (CPSTMF DB table).
        GetPaymentByTransactionIdQuery transactionQuery = new(transactionId);
        var transaction = await _mediator
            .Send(transactionQuery, cancellationToken)
            .ConfigureAwait(false);

        if (transaction is null)
        {
            Log.TransactionNotFound(_logger, networkName, transactionId, rejectionCode);
            throw new ItemNotFoundException("Invalid Transaction Id.");
        }

        // Verify user account has permission to access transaction and network.
        if (!_userInfo.IsAuthenticated)
        {
            Log.RejectionNotAuthenticated(_logger, networkName, transactionId, rejectionCode);
            throw new NotAuthorizedException("Client Id not authenticated.");
        }

        if (!_userInfo.CanAccess(transaction.ClientCode, transaction.BillingEntityCode))
        {
            Log.RejectionNotAllowed(_logger, networkName, transactionId, rejectionCode, transaction.ClientCode, transaction.BillingEntityCode);
            throw new NotAuthorizedException("Client Id not allowed for Client Code and/or Billing Entity Code.");
        }

        // Verify transaction is associated to network (VPTMFCTR DB table).
        GetNetworkByTransactionIdQuery transactionNetworkQuery = new(transactionId);
        var transactionNetwork = await _mediator
            .Send(transactionNetworkQuery, cancellationToken)
            .ConfigureAwait(false);

        if (transactionNetwork == null || !VerifyNetworkName(request, transactionNetwork))
        {
            Log.NetworkNotAssociated(_logger, networkName, transactionId, rejectionCode);
            throw new ItemNotFoundException("Network Partner not associated with transaction.");
        }

        // Verify network payor enrollment
        if (request.VerifyEnrollment)
        {
            // Verify network payor enrollment exists (Preferencing API call).
            GetNetworkPayorEnrollmentQuery payorQuery = new(networkName, transaction.ClientCode, transaction.BillingEntityCode);
            var payor = await _mediator
                .Send(payorQuery, cancellationToken)
                .ConfigureAwait(false);

            if (payor == null)
            {
                Log.PayorNotRecognized(_logger, networkName, transactionId, rejectionCode, transaction.ClientCode, transaction.BillingEntityCode);
                throw new ItemNotFoundException("Payor not recognized or not enrolled with Network.");
            }
        }

        // Request payment rejection to Rest Payment API.
        Log.RejectionRequested(_logger, networkName, transactionId, rejectionCode);
        var reject = new RejectStpPayment.Request(networkName, transactionId, rejectionCode, rejectionReason);
        var response = await _mediator
            .Send(reject, cancellationToken)
            .ConfigureAwait(false);

        switch (response.StatusCode)
        {
            case HttpStatusCode.OK:
                Log.RejectionAccepted(_logger, networkName, transactionId, rejectionCode);
                return new RejectPaymentResult(transactionNetwork.NetworkName, transactionNetwork.TransactionId, $"{response.Content?.ResponseDesc}");

            case HttpStatusCode.BadRequest:
                Log.RejectionValidationError(_logger, networkName, transactionId, rejectionCode, $"{response.Content?.ResponseDesc}");
                throw new FluentValidation.ValidationException(response.Content?.ResponseDesc);

            case HttpStatusCode.Unauthorized:
                Log.RejectionNotAuthenticated(_logger, networkName, transactionId, rejectionCode);
                throw new NotAuthorizedException("Client Id not authenticated.");

            case HttpStatusCode.NotFound:
                Log.TransactionNotFound(_logger, networkName, transactionId, rejectionCode);
                throw new ItemNotFoundException("Invalid Transaction Id.");

            default:
                Log.RejectionUnknownError(_logger, networkName, transactionId, rejectionCode, response.Error?.Message);
                throw new InvalidOperationException(response.Error?.Message);
        }
    }

    private static bool VerifyNetworkName(RejectPaymentCommand request, TransactionNetwork transactionNetwork)
    {
        return string.Equals(transactionNetwork.NetworkName.ToUpperInvariant().Trim(), request.NetworkPartner.ToUpperInvariant(), StringComparison.OrdinalIgnoreCase);
    }

    private static partial class Log
    {
        [LoggerMessage(
            EventId = 0,
            Level = LogLevel.Error,
            Message = "Payment Rejection Request ERROR [Network = '{NetworkName}', Transaction = '{TransactionId}', RejectionCode = {RejectionCode}]",
            SkipEnabledCheck = true)]
        public static partial void RejectionError(ILogger logger, Exception ex, string networkName, long transactionId, string rejectionCode);

        [LoggerMessage(
            EventId = 1,
            Level = LogLevel.Information,
            Message = "Payment Rejection Request ACCEPTED [Network = '{NetworkName}', Transaction = '{TransactionId}', RejectionCode = {RejectionCode}]",
            SkipEnabledCheck = true)]
        public static partial void RejectionAccepted(ILogger logger, string networkName, long transactionId, string rejectionCode);

        [LoggerMessage(
            EventId = 2,
            Level = LogLevel.Warning,
            Message = "Payment Rejection Request DENIED [Network = '{NetworkName}', Transaction = '{TransactionId}', RejectionCode = {RejectionCode}]",
            SkipEnabledCheck = true)]
        public static partial void RejectionDenied(ILogger logger, string networkName, long transactionId, string rejectionCode);

        [LoggerMessage(
            EventId = 3,
            Level = LogLevel.Warning,
            Message = "Payment Rejection Request DENIED [Network = '{NetworkName}', Transaction = '{TransactionId}', RejectionCode = {RejectionCode}] : User Not Authenticated",
            SkipEnabledCheck = true)]
        public static partial void RejectionNotAuthenticated(ILogger logger, string networkName, long transactionId, string rejectionCode);

        [LoggerMessage(
            EventId = 4,
            Level = LogLevel.Warning,
            Message = "Payment Rejection Request DENIED [Network = '{NetworkName}', Transaction = '{TransactionId}', RejectionCode = {RejectionCode}, Client = {ClientCode}, BE = {BillingEntityCode}] : User Not Allowed To Access Transaction",
            SkipEnabledCheck = true)]
        public static partial void RejectionNotAllowed(ILogger logger, string networkName, long transactionId, string rejectionCode, string clientCode, string billingEntityCode);

        [LoggerMessage(
            EventId = 5,
            Level = LogLevel.Warning,
            Message = "Payment Rejection Request DENIED [Network = '{NetworkName}', Transaction = '{TransactionId}', RejectionCode = {RejectionCode}] : Transaction ID Not Found",
            SkipEnabledCheck = true)]
        public static partial void TransactionNotFound(ILogger logger, string networkName, long transactionId, string rejectionCode);

        [LoggerMessage(
            EventId = 6,
            Level = LogLevel.Warning,
            Message = "Payment Rejection Request DENIED [Network = '{NetworkName}', Transaction = '{TransactionId}', RejectionCode = {RejectionCode}] : Network Partner Not Recognized",
            SkipEnabledCheck = true)]
        public static partial void NetworkNotRecognized(ILogger logger, string networkName, long transactionId, string rejectionCode);

        [LoggerMessage(
            EventId = 7,
            Level = LogLevel.Warning,
            Message = "Payment Rejection Request DENIED [Network = '{NetworkName}', Transaction = '{TransactionId}', RejectionCode = {RejectionCode}] : Network Partner Not Associated with Transaction",
            SkipEnabledCheck = true)]
        public static partial void NetworkNotAssociated(ILogger logger, string networkName, long transactionId, string rejectionCode);

        [LoggerMessage(
            EventId = 8,
            Level = LogLevel.Warning,
            Message = "Payment Rejection Request DENIED [Network = '{NetworkName}', Transaction = '{TransactionId}', RejectionCode = {RejectionCode}, Client = {ClientCode}, BE = {BillingEntityCode}] : Network Payor Not Recognized",
            SkipEnabledCheck = true)]
        public static partial void PayorNotRecognized(ILogger logger, string networkName, long transactionId, string rejectionCode, string clientCode, string billingEntityCode);

        [LoggerMessage(
            EventId = 9,
            Level = LogLevel.Warning,
            Message = "Payment Rejection Request DENIED [Network = '{NetworkName}', Transaction = '{TransactionId}', RejectionCode = {RejectionCode}, Provider = {ProviderTin}, Enrollment = {EnrollmentType}] : Network Provider Not Recognized",
            SkipEnabledCheck = true)]
        public static partial void ProviderNotRecognized(ILogger logger, string networkName, long transactionId, string rejectionCode, string providerTin, string enrollmentType);

        [LoggerMessage(
            EventId = 10,
            Level = LogLevel.Warning,
            Message = "Payment Rejection Request DENIED [Network = '{NetworkName}', Transaction = '{TransactionId}', RejectionCode = {RejectionCode}] : {ErrorMessage}",
            SkipEnabledCheck = true)]
        public static partial void RejectionValidationError(ILogger logger, string networkName, long transactionId, string rejectionCode, string? errorMessage);

        [LoggerMessage(
            EventId = 11,
            Level = LogLevel.Warning,
            Message = "Payment Rejection Request ERROR [Network = '{NetworkName}', Transaction = '{TransactionId}', RejectionCode = {RejectionCode}] : {ErrorMessage}",
            SkipEnabledCheck = true)]
        public static partial void RejectionUnknownError(ILogger logger, string networkName, long transactionId, string rejectionCode, string? errorMessage);

        [LoggerMessage(
            EventId = 12,
            Level = LogLevel.Information,
            Message = "Payment Rejection Request REQUESTED [Network = '{NetworkName}', Transaction = '{TransactionId}', RejectionCode = {RejectionCode}]",
            SkipEnabledCheck = true)]
        public static partial void RejectionRequested(ILogger logger, string networkName, long transactionId, string rejectionCode);
    }
}
