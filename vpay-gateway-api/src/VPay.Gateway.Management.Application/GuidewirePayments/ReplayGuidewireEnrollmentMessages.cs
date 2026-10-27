using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using VPay.Gateway.ClientProcessor.Rabbit;
using VPay.Gateway.Common.Guidewire.Models;
using VPay.Gateway.Data.Sql.Exceptions;
using VPay.Gateway.Data.Sql.PepClaims;

namespace VPay.Gateway.Management.Application.GuidewirePayments;

/// <summary>
/// Replays Guidewire enrollment messages by resetting the processing status
/// (<see cref="PayeeStatus.CurrentProcessStatus"/>) to a previous state so they can be reprocessed,
/// and republishes the enrollment message to the ProcessEnrollmentStatusRecords queue.
/// </summary>
public static partial class ReplayGuidewireEnrollmentMessages
{
    /// <summary>
    /// Reset the <see cref="PayeeStatus.CurrentProcessStatus"/> to a previous state.
    /// By default it resets messages in the <see cref="GuidewireEnrollmentMessage.Sending"/>
    /// status to the <see cref="GuidewireEnrollmentMessage.Scheduled"/> so they can be reprocessed.
    /// </summary>
    /// <param name="ClientCode">The optional client code to update, or null to update the status from any client.</param>
    /// <param name="MemberId">The optional member id to update, or null to update the status from any member.</param>
    /// <param name="ClaimId">The optional claim id to update, or null to update the status from any claim.</param>
    /// <param name="CurrentProcessStatus">The current status to reset (<see cref="GuidewireEnrollmentMessage"/>).</param>
    /// <param name="NewProcessStatus">The new intended status (<see cref="GuidewireEnrollmentMessage"/>).</param>
    /// <param name="MessageCount">The maximum number of messages to replay. The default is 1 message.</param>
    /// <param name="MinMessageAgeInMinutes">The minimum message age in minutes in the current status. The default is 30 minutes.</param>
    public record Command(
        string? ClientCode = null,
        string? MemberId = null,
        string? ClaimId = null,
        string CurrentProcessStatus = GuidewireEnrollmentMessage.Sending,
        string NewProcessStatus = GuidewireEnrollmentMessage.Scheduled,
        int MessageCount = 1,
        int MinMessageAgeInMinutes = 30,
        int MaxMessageAgeInDays = 60)
        : IRequest<GuidewireResult>;

    public class Handler : IRequestHandler<Command, GuidewireResult>
    {
        private readonly IMediator _mediator;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<Handler> _logger;

        public Handler(
            IMediator mediator,
            IPublishEndpoint publishEndpoint,
            ILogger<Handler> logger)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<GuidewireResult> Handle(Command request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            // Reset from the Sending => Scheduled status
            Log.ProcessStatusReplayStarted(_logger, request.MessageCount, request.CurrentProcessStatus, request.NewProcessStatus);
            var command = new EnrollmentStatusUpdate.Command(
                request.CurrentProcessStatus,
                request.NewProcessStatus,
                request.ClientCode,
                request.MemberId,
                request.ClaimId,
                request.MessageCount,
                request.MinMessageAgeInMinutes,
                request.MaxMessageAgeInDays);

            var response = await _mediator.Send(command, cancellationToken).ConfigureAwait(false);
            if (response == null)
            {
                Log.ProcessStatusReplayFailed(_logger, request.MessageCount, request.CurrentProcessStatus, request.NewProcessStatus);
                throw new UpdateDidNotOccurException("Unable to replay processing status of Guidewire enrollment message(s)");
            }

            if (!response.Any())
            {
                Log.ProcessStatusReplayNotFound(_logger, request.ClaimId ?? "ANY", request.CurrentProcessStatus, request.NewProcessStatus);
                return new GuidewireResult
                {
                    Message = $"No Guidewire enrollment message(s) found to reset to {request.NewProcessStatus} status",
                    Status = request.NewProcessStatus
                };
            }

            foreach (var item in response)
            {
                await _publishEndpoint
                    .Publish(new ProcessEnrollmentStatusRecords(item.ClientCode, item.MemberId), cancellationToken)
                    .ConfigureAwait(false);

                LogProcessStatusResetCompletion(request, item);

                // NOTE: Last attempt may still succeed
                if (item.RetryCount >= 5)
                {
                    Log.ProcessStatusMaxRetriesExceeded(
                        _logger,
                        item.CurrentProcessStatus ?? "",
                        item.ClientCode ?? "",
                        item.MemberId ?? "");
                }
            }

            var claims = string.Join(',', response.Select(x => x.ClaimId));
            return new GuidewireResult
            {
                Message = $"Successfully reset {response.Count()} Guidewire enrollment message(s) to {request.NewProcessStatus} status ({claims})",
                Status = request.NewProcessStatus
            };
        }

        private void LogProcessStatusResetCompletion(Command request, EnrollmentStatus? item)
        {
            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["ClientCode"] = item?.ClientCode ?? string.Empty,
                ["MemberId"] = item?.MemberId ?? string.Empty,
                ["ClaimId"] = item?.ClaimId ?? string.Empty
            });

            Log.ProcessStatusReplayCompleted(_logger, item?.ClaimId ?? string.Empty, request.CurrentProcessStatus, request.NewProcessStatus);
        }
    }

    private static partial class Log
    {
        [LoggerMessage(
            EventId = 0,
            Level = LogLevel.Error,
            Message = "ERROR replaying Guidewire enrollment processing status (up to {Count}) from '{PreviousStatus}' to '{NewStatus}'")]
        public static partial void ProcessStatusReplayFailed(ILogger logger, int count, string previousStatus, string newStatus);

        [LoggerMessage(
            EventId = 10,
            Level = LogLevel.Debug,
            Message = "START replaying Guidewire enrollment processing status (up to {Count}) from '{PreviousStatus}' to '{NewStatus}'")]
        public static partial void ProcessStatusReplayStarted(ILogger logger, int count, string previousStatus, string newStatus);

        [LoggerMessage(
            EventId = 20,
            Level = LogLevel.Information,
            Message = "REPLAY enrollment Guidewire processing status for claim '{ClaimId}' from '{PreviousStatus}' to '{NewStatus}'")]
        public static partial void ProcessStatusReplayCompleted(ILogger logger, string claimId, string previousStatus, string newStatus);

        [LoggerMessage(
            EventId = 40,
            Level = LogLevel.Information,
            Message = "NOT FOUND Guidewire enrollment processing status for claim '{ClaimId}' from '{PreviousStatus}' to '{NewStatus}'")]
        public static partial void ProcessStatusReplayNotFound(ILogger logger, string claimId, string previousStatus, string newStatus);

        [LoggerMessage(
            EventId = 50,
            Level = LogLevel.Critical,
            Message = "CRITICAL Guidewire enrollment has REACHED MAX RETRIES and won't be replayed again. '{NewStatus}' notification to Guidewire for Client '{ClientCode}', Member Id '{MemberId}'",
            SkipEnabledCheck = true)]
        public static partial void ProcessStatusMaxRetriesExceeded(ILogger logger, string newStatus, string clientCode, string memberId);
    }
}
