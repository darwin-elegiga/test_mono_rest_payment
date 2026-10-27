using MassTransit;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Gateway.Common.Commands;
using VPay.Gateway.Common.PepClaims;
using VPay.Gateway.Common.PepClaims.Models;
using VPay.Gateway.Common.PlatformConfig;
using VPay.Gateway.Data.Connection;
using VPay.Gateway.Data.Sql.PepClaims;

namespace VPay.Gateway.ClientProcessor.Rabbit;

/// <summary>
///This consumer is responsible for handling a single PEP (MEP) Claim update.
/// </summary>
public partial class ProcessPepClaimRecordConsumer : IConsumer<ProcessPepClaimRecord>
{
    private readonly IPepClaimsApi _pepClaimsApi;
    private readonly IMediator _mediator;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IDataConnection<SqlConnection> _connection;
    private readonly ILogger<ProcessPepClaimRecordConsumer> _logger;

    /// <summary>
    /// Claims can have a lot of different statuses.  We only need to track a subset of those statuses.
    /// </summary>
    private readonly IReadOnlySet<string> _claimStatusesToTrack = new HashSet<string>
    {
        MepEnrollmentClaimStatuses.EnrollmentReceived,
        MepEnrollmentClaimStatuses.EnrollmentInProgress,
        MepEnrollmentClaimStatuses.EnrollmentSuccess,
        MepEnrollmentClaimStatuses.EnrollmentExpired,
        MepEnrollmentClaimStatuses.EnrollmentFailed
    };

    /// <summary>
    /// Parties can have a lot of different statuses.  We only need to track a subset of those statuses.
    /// </summary>
    private readonly IReadOnlySet<string> _partyStatusesToTrack = new HashSet<string>
    {
        MepEnrollmentPartyStatuses.UnableToSendEmail,
        MepEnrollmentPartyStatuses.EmailNotDeliverable,
        MepEnrollmentPartyStatuses.EnrollmentLinkExpired,
        MepEnrollmentPartyStatuses.PartyDetailsRejected,
        MepEnrollmentPartyStatuses.EnrollmentFailed,
        MepEnrollmentPartyStatuses.InvalidEftInformation,
        MepEnrollmentPartyStatuses.IdentityVerificationUnsuccessful,
        MepEnrollmentPartyStatuses.PotentiallySanctioned,
        MepEnrollmentPartyStatuses.TermsAndConditionsRejected
    };

    public ProcessPepClaimRecordConsumer(
        IPepClaimsApi pepClaimsApi,
        IMediator mediator,
        IPublishEndpoint publishEndpoint,
        IDataConnection<SqlConnection> connection,
        ILogger<ProcessPepClaimRecordConsumer> logger)
    {
        _pepClaimsApi = pepClaimsApi;
        _mediator = mediator;
        _publishEndpoint = publishEndpoint;
        _connection = connection;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ProcessPepClaimRecord> context)
    {
        var message = context.Message;

        using var logScope = _logger.BeginScope(new Dictionary<string, object> { ["PepClaimId"] = message.ClaimId });

        var claim = await _pepClaimsApi.GetClaimById(message.ClaimId, context.CancellationToken).ConfigureAwait(false);

        // if the claim is not an "enrollment" type, we can exit early
        if (claim.EnrollmentType != "E")
        {
            Log.SkippingClaim(_logger, message.ClaimId);
            return;
        }

        var externalClientConfiguration = await _mediator.Send(
                new ExternalClientConfigurationQuery(claim.Tpa, DateTime.Today),
                context.CancellationToken)
            .ConfigureAwait(false);
        if (externalClientConfiguration == null)
        {
            // If there is no configuration for the client, they're not set up to send to GW
            Log.ExternalClientConfigurationNotFound(_logger, claim.Tpa);
            return;
        }

        // get any statuses we want to track, ordered by date
        var trackedStatuses = claim.ClaimStatuses
            .Where(claimStatus => _claimStatusesToTrack.Contains(claimStatus.Status))
            .OrderBy(claimStatus => claimStatus.Created)
            .ToList()
            .AsReadOnly();

        // if there are no statuses we care about tracking, we can exit early
        if (trackedStatuses.Count == 0)
        {
            return;
        }

        Log.TrackingClaim(_logger, message.ClaimId);

        using var transaction = await _connection.BeginTransaction().ConfigureAwait(false);
        foreach (var party in claim.Parties)
        {
            Payee payee = await GetOrCreatePayee(claim, party, context.CancellationToken).ConfigureAwait(false);

            bool shouldFireUpdate = false;
            for (int index = 0; index < trackedStatuses.Count; index++)
            {
                ClaimStatusDto claimStatus = trackedStatuses[index];
                ClaimStatusDto nextClaimStatus = index + 1 < trackedStatuses.Count
                    ? trackedStatuses[index + 1]
                    : null;

                // get all the party statuses that changed during the time the claim status was active
                var partyStatuses = party.PartyStatuses
                    .Where(partyStatus =>
                        _partyStatusesToTrack.Contains(partyStatus.Status)
                        && partyStatus.Created >= claimStatus.Created
                        && (nextClaimStatus is null || partyStatus.Created < nextClaimStatus.Created))
                    .OrderBy(partyStatus => partyStatus.Created)
                    .ToList();

                // if the claim date doesn't have a match to a party status date, we'll
                // insert the claim change as it's own record
                if (partyStatuses.Count == 0 || partyStatuses[0].Created != claimStatus.Created)
                {
                    shouldFireUpdate |= await SaveClaimPartyStatus(
                            payee,
                            claim,
                            claimStatus,
                            party,
                            partyStatus: null,
                            cancellationToken: context.CancellationToken)
                        .ConfigureAwait(false);
                }

                // for each change in party status, insert that record
                foreach (var partyStatus in partyStatuses)
                {
                    shouldFireUpdate |= await SaveClaimPartyStatus(
                            payee,
                            claim,
                            claimStatus,
                            party,
                            partyStatus,
                            context.CancellationToken)
                        .ConfigureAwait(false);
                }
            }

            if (shouldFireUpdate)
            {
                await _publishEndpoint.Publish(
                        new ProcessEnrollmentStatusRecords(claim.Tpa, party.MemberId),
                        context.CancellationToken)
                    .ConfigureAwait(false);
            }
        }

        transaction.Commit();
    }

    private async Task<bool> SaveClaimPartyStatus(
        Payee payee,
        PepClaimInfoDto claim,
        ClaimStatusDto claimStatus,
        PartyDto party,
        PartyStatusDto partyStatus,
        CancellationToken cancellationToken)
    {
        var payeeStatus = await _mediator.Send(
                new PayeeStatusQuery(payee.Id, claim.Id, claimStatus.Status, partyStatus?.Status),
                cancellationToken)
            .ConfigureAwait(false);
        if (payeeStatus != null)
        {
            return false;
        }

        _ = await _mediator.Send(
                new InsertPayeeStatusCommand(
                    payee.Id,
                    claim.Id,
                    party.Id,
                    claimStatus.Status,
                    EnrollmentNotificationStatus.Scheduled.ToString(),
                    claimStatus.Id,
                    claimStatus.Created,
                    partyStatus?.Id,
                    partyStatus?.Status),
                cancellationToken)
            .ConfigureAwait(false);

        return true;
    }

    private async Task<Payee> GetOrCreatePayee(
        PepClaimInfoDto claim,
        PartyDto party,
        CancellationToken cancellationToken)
    {
        var payee = await _mediator.Send(new PayeeQuery(claim.Tpa, party.MemberId), cancellationToken)
            .ConfigureAwait(false);

        // if the payee is not found, insert it
        payee ??= await _mediator.Send(
                new InsertPayeeCommand(
                    claim.Origin,
                    claim.Tpa,
                    party.MemberId,
                    party.PartyName,
                    party.PartyEmail,
                    party.Phone1),
                cancellationToken)
            .ConfigureAwait(false);

        return payee;
    }

    private static partial class Log
    {
        [LoggerMessage(
            EventId = 0,
            Level = LogLevel.Information,
            Message = "Cannot find a configuration for {ClientCode}",
            SkipEnabledCheck = true)]
        public static partial void ExternalClientConfigurationNotFound(ILogger logger, string clientCode);

        [LoggerMessage(
            EventId = 1,
            Level = LogLevel.Information,
            Message = "Skip {PepClaimId}",
            SkipEnabledCheck = true)]
        public static partial void SkippingClaim(ILogger logger, long pepClaimId);

        [LoggerMessage(
            EventId = 2,
            Level = LogLevel.Information,
            Message = "{PepClaimId} needs to be tracked",
            SkipEnabledCheck = true)]
        public static partial void TrackingClaim(ILogger logger, long pepClaimId);
    }
}
