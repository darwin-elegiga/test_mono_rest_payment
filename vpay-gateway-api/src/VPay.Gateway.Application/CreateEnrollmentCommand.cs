using MediatR;
using Microsoft.Extensions.Logging;
using VPay.Extensions.Exceptions;
using VPay.Gateway.Common.Enrollments;
using VPay.Gateway.Common.Enrollments.Models;
using VPay.Gateway.Common.PlatformConfig;

namespace VPay.Gateway.Application;

public sealed record CreateEnrollmentCommand(
    string ClientCode,
    string BillingEntityCode,
    string PayeeName,
    string PayeeEmail,
    string MemberId,
    string PolicyNumber,
    string CellPhoneNumber
) : IRequest;

public partial class CreateEnrollmentCommandHandler : IRequestHandler<CreateEnrollmentCommand>
{
    private readonly IEnrollmentsApi _enrollmentsApi;
    private readonly IUserInfo _userInfo;
    private readonly ILogger<CreateEnrollmentCommandHandler> _logger;
    private readonly IMediator _mediator;

    public CreateEnrollmentCommandHandler(
        IEnrollmentsApi enrollmentsApi,
        ILogger<CreateEnrollmentCommandHandler> logger,
        IMediator mediator,
        IUserInfo userInfo)
    {
        _enrollmentsApi = enrollmentsApi ?? throw new ArgumentNullException(nameof(enrollmentsApi));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _userInfo = userInfo ?? throw new ArgumentNullException(nameof(userInfo));
    }

    public async Task Handle(CreateEnrollmentCommand request, CancellationToken cancellationToken)
    {
        var findOrigin = await _mediator.Send(new OriginClientQuery(request.ClientCode), cancellationToken).ConfigureAwait(false);
        if (findOrigin?.Origin == null)
        {
            throw new ArgumentException("Could not find ClientCode", nameof(request.ClientCode));
        }

        if (!_userInfo.CanAccess(request.ClientCode, request.BillingEntityCode))
        {
            Log.AccessDenied(_logger, request.ClientCode, request.BillingEntityCode);
            throw new NotAuthorizedException("Client is not enabled for enrollments");
        }

        Log.VerifyingClientIsEnabledForEnrollments(_logger, request.ClientCode, request.BillingEntityCode, request.MemberId);
        var enrollmentConfiguration = await _mediator.Send(new EnrollmentConfigurationQuery(findOrigin.Origin.OriginCode, request.ClientCode), cancellationToken).ConfigureAwait(false);
        if (enrollmentConfiguration?.IsEnrolled != true)
        {
            Log.ClientIsNotEnabledForEnrollments(_logger, request.ClientCode, request.BillingEntityCode, request.MemberId);
            throw new NotAuthorizedException("Client is not enabled for enrollments");
        }

        Log.LookingForExistingEnrollment(_logger, request.ClientCode, request.BillingEntityCode, request.MemberId);
        var hasActiveEnrollment = await _mediator.Send(new HasActiveEnrollmentQuery(request.ClientCode, request.BillingEntityCode, request.MemberId), cancellationToken).ConfigureAwait(false);
        if (hasActiveEnrollment)
        {
            Log.ExistingEnrollmentFound(_logger, request.ClientCode, request.BillingEntityCode, request.MemberId);
            throw new EnrollmentInProgressException(request.ClientCode, request.MemberId);
        }

        // TODO: How do we get the full-color logo?
        // TODO: Do we allow them to pass in the timeout date, or get it from a config somewhere?  Currently set to 7 days from "today"
        var partyEnrollmentRequest = new PartyEnrollmentRequest
        {
            Origin = findOrigin.Origin.OriginCode,
            TPA = request.ClientCode,
            BillingEntity = request.BillingEntityCode,
            PayeeName = request.PayeeName,
            PayeeEmail = request.PayeeEmail,
            Phone1 = request.CellPhoneNumber,
            Phone1Type = "cell",
            MemberID = request.MemberId,
            PolicyNumber = request.PolicyNumber,
            FullColorLogo = string.Format("/home/bogner/{0}/{0}-full-color-logo.jpg", request.ClientCode.ToLowerInvariant()),
            TimeoutDate = DateTime.Today.AddDays(7).ToString("yyyy-MM-dd"),
            DateOfFileTimestamp = DateTime.Now.ToString("yyyy-MM-dd-HH.mm.ss.ffffff")
        };

        var enrollmentRequest = new EnrollmentRequest { PayeeList = { partyEnrollmentRequest } };

        await _enrollmentsApi.CreateEnrollment(enrollmentRequest, cancellationToken).ConfigureAwait(false);
    }

    private static partial class Log
    {
        [LoggerMessage(
            EventId = 0,
            Level = LogLevel.Information,
            Message = "Client does not have access to: Client = {ClientCode}, Billing Entity = {BillingEntityCode}")]
        public static partial void AccessDenied(ILogger logger, string clientCode, string billingEntityCode);

        [LoggerMessage(
            EventId = 1,
            Level = LogLevel.Information,
            Message = "Checking client is enabled for enrollments for Client = {ClientCode}, Billing Entity = {BillingEntityCode}, and Member ID: {MemberId}")]
        public static partial void VerifyingClientIsEnabledForEnrollments(ILogger logger, string clientCode, string billingEntityCode, string memberId);

        [LoggerMessage(
            EventId = 2,
            Level = LogLevel.Information,
            Message = "Client is not enabled for enrollments: Client = {ClientCode}, Billing Entity = {BillingEntityCode}, and Member ID: {MemberId}")]
        public static partial void ClientIsNotEnabledForEnrollments(ILogger logger, string clientCode, string billingEntityCode, string memberId);

        [LoggerMessage(
            EventId = 3,
            Level = LogLevel.Information,
            Message = "Checking for existing enrollment for Client = {ClientCode}, Billing Entity = {BillingEntityCode}, and Member ID: {MemberId}")]
        public static partial void LookingForExistingEnrollment(ILogger logger, string clientCode, string billingEntityCode, string memberId);

        [LoggerMessage(
            EventId = 4,
            Level = LogLevel.Information,
            Message = "Existing enrollment found for Client = {ClientCode}, Billing Entity = {BillingEntityCode}, and Member ID: {MemberId}")]
        public static partial void ExistingEnrollmentFound(ILogger logger, string clientCode, string billingEntityCode, string memberId);
    }
}
