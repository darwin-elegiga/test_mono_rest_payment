using MassTransit;
using MediatR;
using VPay.Gateway.Common.Guidewire;
using VPay.Gateway.Common.Guidewire.Models;
using VPay.Gateway.Common.PlatformConfig;
using VPay.Gateway.Data.Sql.Exceptions;
using VPay.Gateway.Data.Sql.PepClaims;

namespace VPay.Gateway.ClientProcessor.Rabbit;

/// <summary>
/// This consumer is responsible for processing the enrollment status updates for a MEP Claim.
/// </summary>
public partial class ProcessEnrollmentStatusRecordsConsumer : IConsumer<ProcessEnrollmentStatusRecords>
{
    private readonly IMediator _mediator;
    private readonly ILogger<ProcessEnrollmentStatusRecordsConsumer> _logger;

    private static readonly EnrollmentNotificationStatus[] _terminalStatuses =
    {
        EnrollmentNotificationStatus.Completed,
        EnrollmentNotificationStatus.Skipped,
        EnrollmentNotificationStatus.MarkedCompleted
    };

    public ProcessEnrollmentStatusRecordsConsumer(
        IMediator mediator,
        ILogger<ProcessEnrollmentStatusRecordsConsumer> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ProcessEnrollmentStatusRecords> context)
    {
        using var logScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["ClientCode"] = context.Message.ClientCode,
            ["MemberId"] = context.Message.MemberId
        });

        // get the payee associated with the enrollment messages
        var payee = await _mediator.Send(new PayeeQuery(context.Message.ClientCode, context.Message.MemberId), context.CancellationToken).ConfigureAwait(false);
        if (payee == null)
        {
            Log.PayeeNotFound(_logger, context.Message.ClientCode, context.Message.MemberId);
            return;
        }

        // get the configuration for where to send messages
        var externalClientConfiguration = await _mediator.Send(new ExternalClientConfigurationQuery(context.Message.ClientCode, DateTime.Now), context.CancellationToken).ConfigureAwait(false);
        if (externalClientConfiguration == null)
        {
            Log.ClientNotConfiguredForGuidewire(_logger, context.Message.ClientCode);
            return;
        }

        var guidewireSettings = new GuidewireSettings
        {
            Url = externalClientConfiguration.Url,
            UserName = externalClientConfiguration.Username,
            Password = externalClientConfiguration.Password
        };

        await ProcessRecords(context, payee, guidewireSettings).ConfigureAwait(false);
    }

    private async Task ProcessRecords(ConsumeContext<ProcessEnrollmentStatusRecords> context, Payee payee, GuidewireSettings guidewireSettings)
    {
        // get the status records for the payee
        var records = await _mediator.Send(new PayeeStatusesQuery(context.Message.ClientCode, context.Message.MemberId), context.CancellationToken).ConfigureAwait(false);
        if (records.Count == 0)
        {
            Log.NoEnrollmentStatusRecordsFound(_logger, context.Message.ClientCode, context.Message.MemberId);
            return;
        }

        do
        {
            // get the records that aren't in a terminal status
            IEnumerable<PayeeStatus> recordsToProcess =
                from record in records
                where !_terminalStatuses.Any(status => status.Name.Equals(record.CurrentProcessStatus))
                orderby record.StatusDateTime
                select record;

            foreach (var record in recordsToProcess)
            {
                using var logScope = _logger.BeginScope(new Dictionary<string, object>
                {
                    ["ClaimStatus"] = record.Status,
                    ["EnrollmentStatus"] = record.PartyStatus
                });

                // if the record is currently in the "Sending" phase, this consumer can stop here
                if (record.CurrentProcessStatus == EnrollmentNotificationStatus.Sending.Name)
                {
                    return;
                }

                var enrollmentNotificationStatus = record.Status == MepEnrollmentClaimStatuses.EnrollmentReceived
                    ? EnrollmentNotificationStatus.Skipped
                    : EnrollmentNotificationStatus.Started;
                await UpdateRecordStatus(record, record.CurrentProcessStatus, enrollmentNotificationStatus.ToString(), context.CancellationToken).ConfigureAwait(false);

                if (record.Status == MepEnrollmentClaimStatuses.EnrollmentReceived)
                {
                    // The "Enrollment Received" status doesn't trigger a notification to Guidewire, so we can move on to the next item
                    continue;
                }

                // Move from the Started => Sending status
                await UpdateRecordStatus(record, EnrollmentNotificationStatus.Started.ToString(), EnrollmentNotificationStatus.Sending.ToString(), context.CancellationToken).ConfigureAwait(false);

                // get the appropriate guidewire status for the enrollment status
                GuidewireEnrollmentStatus enrollmentStatus = ConvertClaimStatusToGuidewireEnrollmentStatus(record.Status);
                string reasonDescription = ConvertPartyStatusToGuidewireReason(record.PartyStatus);

                GuidewireResult result;
                try
                {
                    // try to send the message to Guidewire
                    Log.SendingNotification(_logger);
                    result = await _mediator.Send(new SendEnrollmentStatusChangeCommand(enrollmentStatus, payee.MemberId, payee.Name, reasonDescription, guidewireSettings), context.CancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    // NOTE: We "LogCritical" here because we won't hit a FaultConsumer in this case, but we want to know if there's an issue sending to Guidewire
                    Log.ErrorEncounteredSendingNotification(_logger, ex, enrollmentStatus, context.Message.ClientCode, context.Message.MemberId);

                    // NOTE: We don't want to re-throw the error here, because we don't want to trigger any retries or error queue messages
                    return;
                }

                // update the record with the appropriate status based on the response from Guidewire
                enrollmentNotificationStatus = result.Status == "SUCCESS"
                    ? EnrollmentNotificationStatus.Completed
                    : EnrollmentNotificationStatus.Failed;

                await UpdateRecordStatus(record, EnrollmentNotificationStatus.Sending.ToString(), enrollmentNotificationStatus.ToString(), context.CancellationToken).ConfigureAwait(false);

                // if the enrollment notification to Guidewire failed, log an error and exit
                if (enrollmentNotificationStatus == EnrollmentNotificationStatus.Failed)
                {
                    Log.ReceivedFailedResponseToSendingNotification(_logger, enrollmentStatus, context.Message.ClientCode, context.Message.MemberId);
                    return;
                }
            }

            // check if there are any new records that should be processed for this client/member id
            records = await _mediator.Send(new PayeeStatusesQuery(context.Message.ClientCode, context.Message.MemberId), context.CancellationToken).ConfigureAwait(false);
        } while (!records.All(x => _terminalStatuses.Any(status => status.Name.Equals(x.CurrentProcessStatus))));
    }

    private async Task UpdateRecordStatus(PayeeStatus entity, string previousStatus, string newStatus, CancellationToken cancellationToken)
    {
        Log.UpdatingStatus(_logger, previousStatus, newStatus);
        var updatedEntity = await _mediator.Send(new UpdatePayeeStatusCommand(entity.Id, newStatus, previousStatus), cancellationToken).ConfigureAwait(false);
        if (updatedEntity == null)
        {
            Log.StatusUpdateFailed(_logger, newStatus);
            throw new UpdateDidNotOccurException($"Update to Status = '{newStatus}' failed for Payee Status Record (Id = {entity.Id})");
        }
    }

    private static GuidewireEnrollmentStatus ConvertClaimStatusToGuidewireEnrollmentStatus(string claimStatus)
    {
        return claimStatus switch
        {
            MepEnrollmentClaimStatuses.EnrollmentInProgress => GuidewireEnrollmentStatus.InProgress,
            MepEnrollmentClaimStatuses.EnrollmentSuccess => GuidewireEnrollmentStatus.Enrolled,
            MepEnrollmentClaimStatuses.EnrollmentExpired => GuidewireEnrollmentStatus.Failed,
            MepEnrollmentClaimStatuses.EnrollmentFailed => GuidewireEnrollmentStatus.Failed,
            _ => throw new InvalidOperationException($"Unknown enrollment status '{claimStatus}'")
        };
    }

    private static string ConvertPartyStatusToGuidewireReason(string partyStatus)
    {
        return partyStatus switch
        {
            _ when string.IsNullOrWhiteSpace(partyStatus) => string.Empty,
            MepEnrollmentPartyStatuses.UnableToSendEmail =>
                "ACH enrollment failed because the email could not be sent",
            MepEnrollmentPartyStatuses.EmailNotDeliverable =>
                "ACH enrollment failed because the email was returned as undeliverable",
            MepEnrollmentPartyStatuses.EnrollmentLinkExpired =>
                "ACH enrollment failed because the enrollment link expired",
            MepEnrollmentPartyStatuses.PartyDetailsRejected =>
                "ACH enrollment failed because party details have been rejected",
            MepEnrollmentPartyStatuses.EnrollmentFailed =>
                "ACH enrollment failed because too many failed validation / authentication attempts",
            MepEnrollmentPartyStatuses.InvalidEftInformation =>
                "ACH enrollment failed because invalid bank account details were provided",
            MepEnrollmentPartyStatuses.IdentityVerificationUnsuccessful =>
                "ACH enrollment failed because identity could not be validated",
            MepEnrollmentPartyStatuses.PotentiallySanctioned =>
                "ACH enrollment failed because person is potentially on a sanctions list",
            MepEnrollmentPartyStatuses.TermsAndConditionsRejected =>
                "ACH enrollment failed because T+Cs have been rejected",
            _ => throw new InvalidOperationException($"Unknown party status '{partyStatus}'")
        };
    }

    private static partial class Log
    {
        [LoggerMessage(
            EventId = 0,
            Level = LogLevel.Information,
            Message = "Payee not found for Client '{ClientCode}', Member Id '{MemberId}'",
            SkipEnabledCheck = true)]
        public static partial void PayeeNotFound(ILogger logger, string clientCode, string memberId);

        [LoggerMessage(
            EventId = 1,
            Level = LogLevel.Information,
            Message = "Client '{ClientCode}' is not configured to send to Guidewire",
            SkipEnabledCheck = true)]
        public static partial void ClientNotConfiguredForGuidewire(ILogger logger, string clientCode);

        [LoggerMessage(
            EventId = 2,
            Level = LogLevel.Information,
            Message = "No enrollment status records found for processing for Client '{ClientCode}', Member Id '{MemberId}'",
            SkipEnabledCheck = true)]
        public static partial void NoEnrollmentStatusRecordsFound(ILogger logger, string clientCode, string memberId);

        [LoggerMessage(
            EventId = 3,
            Level = LogLevel.Debug,
            Message = "Sending notification to Guidewire")]
        public static partial void SendingNotification(ILogger logger);

        [LoggerMessage(
            EventId = 4,
            Level = LogLevel.Critical,
            Message = "Error encountered while sending '{GuidewireEnrollmentStatus}' notification to Guidewire for Client '{ClientCode}', Member Id '{MemberId}'",
            SkipEnabledCheck = true)]
        public static partial void ErrorEncounteredSendingNotification(ILogger logger, Exception exception, GuidewireEnrollmentStatus guidewireEnrollmentStatus, string clientCode, string memberId);

        [LoggerMessage(
            EventId = 5,
            Level = LogLevel.Debug,
            Message = "Updating status from '{PreviousStatus}' to '{NewStatus}'")]
        public static partial void UpdatingStatus(ILogger logger, string previousStatus, string newStatus);

        [LoggerMessage(
            EventId = 6,
            Level = LogLevel.Error,
            Message = "Update to Status = '{GuidewireEnrollmentStatus}' failed")]
        public static partial void StatusUpdateFailed(ILogger logger, string guidewireEnrollmentStatus);

        [LoggerMessage(
            EventId = 7,
            Level = LogLevel.Critical,
            Message = "CRITICAL Guidewire Service responded FAILED '{GuidewireEnrollmentStatus}' notification to Guidewire for Client '{ClientCode}', Member Id '{MemberId}'",
            SkipEnabledCheck = true)]
        public static partial void ReceivedFailedResponseToSendingNotification(ILogger logger, GuidewireEnrollmentStatus guidewireEnrollmentStatus, string clientCode, string memberId);

        [LoggerMessage(
            EventId = 8,
            Level = LogLevel.Critical,
            Message = "CRITICAL Guidewire Service MAX RETRIES REACHED. Message won't be replayed again. '{GuidewireEnrollmentStatus}' notification to Guidewire for Client '{ClientCode}', Member Id '{MemberId}'",
            SkipEnabledCheck = true)]
        public static partial void MaxRetriesExceededResponseToSendingNotification(ILogger logger, GuidewireEnrollmentStatus guidewireEnrollmentStatus, string clientCode, string memberId);
    }
}
