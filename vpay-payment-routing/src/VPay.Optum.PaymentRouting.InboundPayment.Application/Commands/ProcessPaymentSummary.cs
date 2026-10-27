using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using VPay.Extensions.Exceptions;
using VPay.Optum.PaymentRouting.Common.Serialization;
using VPay.Optum.PaymentRouting.Data.Sql.BatchSummary;
using VPay.Optum.PaymentRouting.Data.Sql.InboundBatch.Queries;
using VPay.Optum.PaymentRouting.InboundPayment.Application.Executors;

namespace VPay.Optum.PaymentRouting.InboundPayment.Application.Commands
{
    public static class ProcessPaymentSummary
    {
        public class Command : IRequest<Unit>
        {
            public PaymentSummary PaymentSummary { get; set; }
        }

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
                _logger.LogInformation("Processing payment summary for batch {OptumBatchId}", request.PaymentSummary.BatchId);

                //Transform summary and call mediator to put in DB
                var paymentSummary = request.PaymentSummary;

                if (!int.TryParse(paymentSummary.TotalPaymentCount, out var totalCount) || totalCount < 0)
                {
                    _logger.LogInformation("Batch Total Payment Count invalid");
                    throw new VPayAlertableException(new VPayAlert(
                        "Optum PRS",
                        "Batch Total Payment Count invalid",
                        $"Batch Total Payment Count is invalid for batchId:{paymentSummary.BatchId}",
                        $"batchId:{paymentSummary.BatchId} totalPaymentAmount:{paymentSummary.TotalPaymentCount}"
                    ));
                }

                if (!decimal.TryParse(paymentSummary.TotalPaymentAmount, out var totalAmount) || totalAmount < 0)
                {
                    _logger.LogInformation("Batch Total Payment Amount invalid");
                    throw new VPayAlertableException(new VPayAlert(
                        "Optum PRS",
                        "Batch Total Payment Amount invalid",
                        $"Batch Total Payment Amount is invalid for batchId:{paymentSummary.BatchId}",
                        $"batchId:{paymentSummary.BatchId} totalPaymentAmount:{paymentSummary.TotalPaymentAmount}"
                    ));
                }

                var existingBatch = await _mediator.Send(new GetInboundBatchByOptumBatchIdSql.Query(request.PaymentSummary.BatchId), cancellationToken);

                if (existingBatch?.TotalPaymentAmount != null)
                {
                    if (existingBatch.TotalPaymentAmount != totalAmount || existingBatch.TotalPaymentCount != totalCount)
                    {
                        throw new VPayAlertableException(new VPayAlert(
                            "Optum PRS",
                            "Existing batch found with different totals",
                            $"Existing batch found with different totals batchId:{existingBatch.OptumBatchId}",
                            $"batchId: {existingBatch.OptumBatchId}\n" +
                            $"existing (count/total): {existingBatch.TotalPaymentCount}/{existingBatch.TotalPaymentAmount}\n" +
                            $"incoming (count/total): {totalCount}/{totalAmount} (Note: incoming values are strings before conversion)"
                            ));
                    }
                    _logger.LogInformation("Duplicate batch totals match. Ignoring summary update.");
                    return Unit.Value;
                }

                var command = new InsertInboundBatchSql.Query(paymentSummary.BatchId, paymentSummary.Payer835Id, totalCount, totalAmount);
                var batchSummary = await _mediator.Send(command, cancellationToken);

                await _executor.Execute(batchSummary, cancellationToken);

                return Unit.Value;
            }
        }
    }
}
