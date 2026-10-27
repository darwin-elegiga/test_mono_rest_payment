using System.Diagnostics.CodeAnalysis;
using System.Threading;
using MassTransit;
using VPay.Optum.PaymentRouting.Application.InboundPayments.Commands;
using VPay.Optum.PaymentRouting.Common.RabbitMessages;
using VPay.Optum.PaymentRouting.Data.Sql.BatchSummary;
using VPay.Optum.PaymentRouting.Data.Sql.RemittanceAdviceFile.Commands;
using VPay.Optum.PaymentRouting.InboundPayment.Application.Executors;

namespace VPay.Optum.PaymentRouting.RemittanceAdviceFile.Consumers
{
    [ExcludeFromCodeCoverage]
    public class RemittanceAdviceFileConsumer : IConsumer<RemittanceAdviceFileMessage>
    {
        private readonly ILogger<RemittanceAdviceFileConsumer> _logger;
        private readonly MediatR.IMediator _mediator;
        private readonly ICheckBatchCompleteExecutor _executor;

        public RemittanceAdviceFileConsumer(ILogger<RemittanceAdviceFileConsumer> logger, MediatR.IMediator mediator, ICheckBatchCompleteExecutor executor)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        }

        [ExcludeFromCodeCoverage]
        public Task Consume(ConsumeContext<RemittanceAdviceFileMessage> context)
        {
            RemittanceAdviceFileMessage message = context.Message;
            return Consume(message, CancellationToken.None);
        }

        private async Task Consume(RemittanceAdviceFileMessage message, CancellationToken token)
        {
            _logger.LogInformation("Received RemittanceAdviceFile message for payment {UserKey}", message.UserKey);
            var command = new InsertRemittanceAdviceFile.Command(
                message.Payer835Id,
                message.InboundZipfile,
                message.UserKey,
                message.PayerAlias,
                message.OriginCode,
                message.ClientCode,
                message.BillingEntityCode,
                message.RemitMethod,
                message.PdfExtractPath,
                message.CreatedBy
            );

            // Insert into db
            var result = await _mediator.Send(command, token).ConfigureAwait(false);
            // If there is an existing batch for this PDF, trigger a completion check.
            if (result.IsSuccess && (result.Value?.InboundBatchId != null ))
            {
                long inboundBatchId = result.Value.InboundBatchId.Value;
                var batchSummary = await _mediator.Send(new GetBatchSummarySql.Query(inboundBatchId),token).ConfigureAwait(false);
                await _executor.Execute(batchSummary, token).ConfigureAwait(false);
            }
        }
    }
}
