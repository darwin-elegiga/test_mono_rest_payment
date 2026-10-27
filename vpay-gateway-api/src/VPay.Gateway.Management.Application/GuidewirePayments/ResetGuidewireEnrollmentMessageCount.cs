using System.ServiceModel.Channels;
using MediatR;
using Microsoft.Extensions.Logging;
using VPay.Gateway.Common.Guidewire.Models;
using VPay.Gateway.Data.Sql.Exceptions;
using VPay.Gateway.Data.Sql.PepClaims;

namespace VPay.Gateway.Management.Application.GuidewirePayments;

/// <summary>
/// Resets Guidewire enrollment message retry count.
/// </summary>
public static partial class ResetGuidewireEnrollmentMessageCount
{
    /// <summary>
    /// Reset the <see cref="PayeeStatus.RetryCount"/>.
    /// By default it resets the count to zero.
    /// </summary>
    /// <param name="ClientCode">The client code of the claim to update. Required.</param>
    /// <param name="MemberId">The member id of the claim to update. Required.</param>
    /// <param name="ClaimId">The claim id to update. Required.</param>
    public record Command(
        string ClientCode,
        string MemberId,
        string ClaimId,
        int RetryCount)
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

            var command = new EnrollmentStatusRetryCountUpdate.Command(
                ClientCode: request.ClientCode,
                MemberId: request.MemberId,
                ClaimId: request.ClaimId,
                RetryCount: request.RetryCount);

            var response = await _mediator.Send(command, cancellationToken).ConfigureAwait(false);
            if (response == null)
            {
                Log.ProcessStatusResetCountFailed(_logger, request.RetryCount);
                throw new UpdateDidNotOccurException("Unable to reset message retry count of Guidewire enrollment message(s)");
            }

            LogProcessStatusResetCompletion(request);

            return new GuidewireResult
            {
                Message = $"Successfully reset Guidewire enrollment message retry count to {request.RetryCount} (client {request.ClientCode}, member {request.MemberId}, claim {request.ClaimId})",
                Status = response.CurrentProcessStatus
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

            Log.ProcessStatusResetCountCompleted(_logger, request.ClaimId ?? string.Empty, request.RetryCount);
        }
    }

    private static partial class Log
    {
        [LoggerMessage(
            EventId = 0,
            Level = LogLevel.Error,
            Message = "ERROR resetting enrollment processing retry count to {Count}")]
        public static partial void ProcessStatusResetCountFailed(ILogger logger, int count);

        [LoggerMessage(
            EventId = 10,
            Level = LogLevel.Debug,
            Message = "START resetting enrollment processing retry count to {Count}")]
        public static partial void ProcessStatusResetCountStarted(ILogger logger, int count);

        [LoggerMessage(
            EventId = 20,
            Level = LogLevel.Information,
            Message = "RESET enrollment processing retry count for claim '{ClaimId}' to '{Count}'")]
        public static partial void ProcessStatusResetCountCompleted(ILogger logger, string claimId, int count);
    }
}
