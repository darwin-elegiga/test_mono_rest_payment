using MediatR;
using Microsoft.Extensions.Logging;
using VPay.Gateway.Common.Guidewire.Models;
using VPay.Gateway.Data.Paging;
using VPay.Gateway.Data.Sql.PepClaims;

namespace VPay.Gateway.Management.Application.GuidewirePayments;

/// <summary>
/// Searches for failed Guidewire enrollment messages by inspecting the <see cref="EnrollmentStatus.CurrentProcessStatus"/>.
/// </summary>
public static partial class SearchFailedGuidewireEnrollmentMessages
{
    /// <summary>
    /// Reset the <see cref="EnrollmentStatus.CurrentProcessStatus"/> to a previous state.
    /// By default it resets messages in the <see cref="GuidewireEnrollmentMessage.Sending"/>
    /// status to the <see cref="GuidewireEnrollmentMessage.Scheduled"/> so they can be reprocessed.
    /// </summary>
    /// <param name="ClientCode">The optional client code, or null for any client code.</param>
    /// <param name="MemberId">The optional member id, or null for any members.</param>
    /// <param name="ClaimId">The optional claim Id, or null for any claims.</param>
    /// <param name="ProcessStatus">The optional current process status (<see cref="GuidewireEnrollmentMessage"/>),
    /// or null for any process status.</param>
    /// <param name="Page">The page to return.</param>
    /// <param name="PageSize">The number of items to return per page.</param>
    public record Query(
        string? ClientCode = null,
        string? MemberId = null,
        string? ClaimId = null,
        string? ProcessStatus = GuidewireEnrollmentMessage.Failed,
        int? Page = Defaults.DefaultPage,
        int? PageSize = Defaults.DefaultPageSize)
        : IRequest<PagedResult<EnrollmentStatus>>;

    public class Handler : IRequestHandler<Query, PagedResult<EnrollmentStatus>>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<Handler> _logger;

        public Handler(IMediator mediator, ILogger<Handler> logger)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<PagedResult<EnrollmentStatus>> Handle(Query request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            string processStatus = request.ProcessStatus ?? string.Empty;
            string clientCode = request.ClientCode ?? string.Empty;
            string memberId = request.MemberId ?? string.Empty;
            string claimId = request.ClaimId ?? string.Empty;

            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["Status"] = processStatus,
                ["ClientCode"] = clientCode,
                ["MemberId"] = memberId,
                ["ClaimId"] = claimId
            });

            // Reset from the Sending => Scheduled status
            Log.ProcessStatusSearchStarted(_logger, processStatus);
            var query = new EnrollmentStatusSearch.Query(
                request.ProcessStatus,
                request.ClientCode,
                request.MemberId,
                request.ClaimId,
                request.Page,
                request.PageSize);

            var response = await _mediator.Send(query, cancellationToken).ConfigureAwait(false);

            if (response == null)
            {
                Log.ProcessStatusSearchFailed(_logger, processStatus);
                throw new InvalidOperationException("Unable to retrieve failed Guidewire enrollment messages");
            }

            Log.ProcessStatusResetCompleted(_logger, processStatus);

            return response ?? PagedResult<EnrollmentStatus>.Empty();
        }
    }

    private static partial class Log
    {
        [LoggerMessage(
            EventId = 0,
            Level = LogLevel.Error,
            Message = "ERROR searching failed enrollments with processing status '{Status}'")]
        public static partial void ProcessStatusSearchFailed(ILogger logger, string status);

        [LoggerMessage(
            EventId = 10,
            Level = LogLevel.Debug,
            Message = "START searching failed enrollments with processing status '{Status}'")]
        public static partial void ProcessStatusSearchStarted(ILogger logger, string status);

        [LoggerMessage(
            EventId = 20,
            Level = LogLevel.Debug,
            Message = "FOUND failed enrollments with processing status '{Status}'")]
        public static partial void ProcessStatusResetCompleted(ILogger logger, string status);
    }
}
