using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using VPay.Optum.PaymentRouting.Data.Sql.BatchSummary;
using VPay.Optum.PaymentRouting.InboundPayment.Application.Executors;

namespace VPay.Optum.PaymentRouting.InboundPayment.Application.Commands
{
    public static class CheckBatchCompletion
    {
        public record Command(long InboundBatchId) : IRequest;

        public class Handler : IRequestHandler<Command, Unit>
        {
            private readonly ILogger<Handler> _logger;
            private readonly IMediator _mediator;
            private readonly ICheckBatchCompleteExecutor _executor;

            public Handler(ILogger<Handler> logger, IMediator mediator, ICheckBatchCompleteExecutor executor)
            {
                _logger = logger;
                _mediator = mediator;
                _executor = executor;
            }

            public async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
            {
                _logger.LogInformation("Getting batch summary: {batchId}", request.InboundBatchId);
                var batchSummary = await _mediator.Send(new GetBatchSummarySql.Query(request.InboundBatchId),
                    cancellationToken).ConfigureAwait(false);

                await _executor.Execute(batchSummary, cancellationToken).ConfigureAwait(false);

                return Unit.Value;
            }
        }
    }
}
