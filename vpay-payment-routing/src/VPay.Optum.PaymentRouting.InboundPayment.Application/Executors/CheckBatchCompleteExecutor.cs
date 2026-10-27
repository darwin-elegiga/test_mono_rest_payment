using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using VPay.Optum.PaymentRouting.Application.Models.RabbitMessages;
using VPay.Optum.PaymentRouting.Data.Abstractions;
using VPay.Optum.PaymentRouting.Data.Sql.BatchSummary;

namespace VPay.Optum.PaymentRouting.InboundPayment.Application.Executors
{
    public class CheckBatchCompleteExecutor : ICheckBatchCompleteExecutor
    {
        private readonly ILogger<CheckBatchCompleteExecutor> _logger;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IMediator _mediator;

        public CheckBatchCompleteExecutor(ILogger<CheckBatchCompleteExecutor> logger, IPublishEndpoint publishEndpoint, IMediator mediator)
        {
            _logger = logger;
            _publishEndpoint = publishEndpoint;
            _mediator = mediator;
        }

        public async Task<Unit> Execute(BatchSummary batchSummary, CancellationToken cancellation = default)
        {
            if (batchSummary.IsBatchReceived())
            {
                _logger.LogInformation("Batch is complete {batchId} - {OptumBatchId}", batchSummary.InboundBatchId, batchSummary.OptumBatchId);

                if (batchSummary.Status == BatchStatus.BatchInitiated)
                {
                    _logger.LogInformation("Sending completed rabbit message for batch {batchId} - {OptumBatchId}", batchSummary.InboundBatchId, batchSummary.OptumBatchId);
                    //Mark batch as received
                    var command = new InsertBatchStatus.Command
                    {
                        InboundBatchId = batchSummary.InboundBatchId,
                        Status = BatchStatus.BatchReceived.Name
                    };
                    var rowsAffected = await _mediator.Send(command, cancellation);

                    if (rowsAffected > 0)
                    {
                        _logger.LogInformation("Publishing to queue for batch {batchId} - {OptumBatchId}", batchSummary.InboundBatchId, batchSummary.OptumBatchId);
                        //Publish message for PHP to start processing batch
                        await _publishEndpoint.Publish(new BatchComplete { InboundBatchId = batchSummary.InboundBatchId, OptumBatchId = batchSummary.OptumBatchId }, cancellation).ConfigureAwait(false);
                    }
                }
            }
            else
            {
                _logger.LogInformation("Batch is incomplete {batchId} - {OptumBatchId} and will continue to wait", batchSummary.InboundBatchId, batchSummary.OptumBatchId);
            }

            return await Unit.Task;
        }
    }
}
