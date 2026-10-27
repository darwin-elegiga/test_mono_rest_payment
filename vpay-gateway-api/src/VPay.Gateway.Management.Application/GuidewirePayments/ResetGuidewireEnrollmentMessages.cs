using MediatR;
using Microsoft.Extensions.Logging;
using VPay.Gateway.Common.Guidewire.Models;
using VPay.Gateway.Data.Sql.Exceptions;
using VPay.Gateway.Data.Sql.PepClaims;

namespace VPay.Gateway.Management.Application.GuidewirePayments;

/// <summary>
/// Resets Guidewire enrollment message processing status (<see cref="PayeeStatus.CurrentProcessStatus"/>)
/// to a terminal status (SKIPPED, COMPLETED, or FAILED).
/// </summary>
public static partial class ResetGuidewireEnrollmentMessages
{
    /// <summary>
    /// Reset the <see cref="PayeeStatus.CurrentProcessStatus"/> to a terminal status (SKIPPED, COMPLETED, or FAILED).
    /// By default it resets messages in the <see cref="GuidewireEnrollmentMessage.Sending"/>
    /// status to the <see cref="GuidewireEnrollmentMessage.Skipped"/> so it completes its processing.
    /// </summary>
    /// <param name="ClientCode">The client code of the claim to update. Required.</param>
    /// <param name="MemberId">The member id of the claim to update. Required.</param>
    /// <param name="ClaimId">The claim id to update. Required.</param>
    /// <param name="CurrentProcessStatus">The current status to reset (<see cref="GuidewireEnrollmentMessage"/>).</param>
    /// <param name="NewProcessStatus">The new intended status (<see cref="GuidewireEnrollmentMessage"/>).</param>
    public record Command(
        string ClientCode,
        string MemberId,
        string ClaimId,
        string CurrentProcessStatus = GuidewireEnrollmentMessage.Sending,
        string NewProcessStatus = GuidewireEnrollmentMessage.Skipped)
        : IRequest<GuidewireResult>;

    public class Handler : IRequestHandler<Command, GuidewireResult>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<Handler> _logger;

        public Handler(IMediator mediator, ILogger<Handler> logger)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<GuidewireResult> Handle(Command request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            // Get the payee status record for the requested client, member and claim.
            var records = await _mediator.Send(new PayeeStatusesQuery(request.ClientCode, request.MemberId), cancellationToken).ConfigureAwait(false);
            var record = records.FirstOrDefault(x => x.ClaimId == request.ClaimId && x.CurrentProcessStatus == request.CurrentProcessStatus);
            if (records.Count == 0 || record == null)
            {
                Log.ProcessStatusResetFailed(_logger, 1, request.CurrentProcessStatus, request.NewProcessStatus);
                return new GuidewireResult
                {
                    Message = $"Record not found for Client '{request.ClientCode}', Member Id '{request.MemberId}', Claim Id '{request.ClaimId}'",
                    Status = request.NewProcessStatus
                };
            }

            // Reset from the Sending => Failed status
            Log.ProcessStatusResetStarted(_logger, 1, request.CurrentProcessStatus, request.NewProcessStatus);
            var command = new UpdatePayeeStatusCommand(record.Id, request.NewProcessStatus, request.CurrentProcessStatus);

            var response = await _mediator.Send(command, cancellationToken).ConfigureAwait(false);
            if (response == null)
            {
                Log.ProcessStatusResetFailed(_logger, 1, request.CurrentProcessStatus, request.NewProcessStatus);
                throw new UpdateDidNotOccurException("Unable to reset processing status of Guidewire enrollment message(s)");
            }

            LogProcessStatusResetCompletion(request);

            return new GuidewireResult
            {
                Message = $"Successfully reset Guidewire enrollment message to {request.NewProcessStatus} status (client {request.ClientCode}, member {request.MemberId}, claim {request.ClaimId})",
                Status = request.NewProcessStatus
            };
        }

        private void LogProcessStatusResetCompletion(Command request)
        {
            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["ClientCode"] = request.ClientCode ?? string.Empty,
                ["MemberId"] = request.MemberId ?? string.Empty,
                ["ClaimId"] = request.ClaimId ?? string.Empty
            });

            Log.ProcessStatusResetCompleted(_logger, request.ClaimId ?? string.Empty, request.CurrentProcessStatus, request.NewProcessStatus);
        }
    }

    private static partial class Log
    {
        [LoggerMessage(
            EventId = 0,
            Level = LogLevel.Error,
            Message = "ERROR resetting enrollment processing status (up to {Count}) from '{PreviousStatus}' to '{NewStatus}'")]
        public static partial void ProcessStatusResetFailed(ILogger logger, int count, string previousStatus, string newStatus);

        [LoggerMessage(
            EventId = 10,
            Level = LogLevel.Debug,
            Message = "START resetting enrollment processing status (up to {Count}) from '{PreviousStatus}' to '{NewStatus}'")]
        public static partial void ProcessStatusResetStarted(ILogger logger, int count, string previousStatus, string newStatus);

        [LoggerMessage(
            EventId = 20,
            Level = LogLevel.Information,
            Message = "RESET enrollment processing status for claim '{ClaimId}' from '{PreviousStatus}' to '{NewStatus}'")]
        public static partial void ProcessStatusResetCompleted(ILogger logger, string claimId, string previousStatus, string newStatus);
    }
}
