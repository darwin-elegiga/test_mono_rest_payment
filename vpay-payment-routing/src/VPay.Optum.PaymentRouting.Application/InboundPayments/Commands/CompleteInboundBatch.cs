using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MediatR;
using Microsoft.Extensions.Logging;
using VPay.Optum.PaymentRouting.Data.Abstractions;
using VPay.Optum.PaymentRouting.Data.Sql.InboundBatch.Commands;

namespace VPay.Optum.PaymentRouting.Application.InboundPayments.Commands
{
    public static class CompleteInboundBatch
    {
        public class Command : IRequest
        {
            public long InboundBatchId { get; set; }
        }

        [UsedImplicitly]
        public class Handler : IRequestHandler<Command>
        {
            private readonly ILogger<Handler> _logger;
            private readonly IMediator _mediator;

            public Handler(ILogger<Handler> logger, IMediator mediator)
            {
                _logger = logger;
                _mediator = mediator;
            }

            public async Task<Unit> Handle(Command command, CancellationToken cancellationToken)
            {
                _logger.LogInformation("Marking batch complete with id: {batchId}", command.InboundBatchId);
                await _mediator.Send(new UpdateInboundBatchStatusSql
                    .Command(command.InboundBatchId, BatchStatus.BatchComplete), cancellationToken);
                _logger.LogInformation("Marked batch complete with id: {batchId}", command.InboundBatchId);
                return Unit.Value;
            }
        }
    }
}
