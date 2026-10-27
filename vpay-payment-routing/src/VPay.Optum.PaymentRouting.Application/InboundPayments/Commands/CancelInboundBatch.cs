using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MediatR;
using Microsoft.Extensions.Logging;
using VPay.Optum.PaymentRouting.Data.Abstractions;
using VPay.Optum.PaymentRouting.Data.Sql.BatchManifest;
using VPay.Optum.PaymentRouting.Data.Sql.BatchSummary;
using VPay.Optum.PaymentRouting.Data.Sql.InboundBatch.Commands;

namespace VPay.Optum.PaymentRouting.Application.InboundPayments.Commands
{
    /// <exception cref="InvalidOperationException">when [InboundBatch] record not found for <see cref="InboundBatchId"/></exception>
    /// <exception cref="InvalidOperationException">when [InboundBatch].[Status] is COMPLETE</exception>
    public static class CancelInboundBatch
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

            public async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
            {
                _logger.LogInformation("Marking batch cancelled with id: {batchId}", request.InboundBatchId);

                await EnsureValidity(request.InboundBatchId, cancellationToken);

                await _mediator.Send(new UpdateInboundBatchStatusSql
                    .Command(request.InboundBatchId, BatchStatus.BatchCancelled), cancellationToken);

                _logger.LogInformation("Marked batch cancelled with id: {batchId}", request.InboundBatchId);

                return Unit.Value;
            }

            private async Task EnsureValidity(long inboundBatchId, CancellationToken cancellationToken)
            {
                var batch = await _mediator.Send(new GetBatchSummarySql
                    .Query(inboundBatchId), cancellationToken);

                if (batch == null)
                {
                    throw new InvalidOperationException($"No [InboundBatch] record exists for [Id] = {inboundBatchId}");
                }

                if (batch.Status == BatchStatus.BatchComplete)
                {
                    throw new InvalidOperationException($"Cannot update [InboundBatch].[Status] when [Status] = {BatchStatus.BatchComplete.Name}");
                }
            }
        }
    }
}
