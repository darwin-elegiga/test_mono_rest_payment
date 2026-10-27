using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using MediatR;
using VPay.Optum.PaymentRouting.Common.RabbitMessages;
using VPay.Optum.PaymentRouting.InboundPayment.Application.Commands;

namespace VPay.Optum.PaymentRouting.InboundPayment.Consumers.Consumers.CheckCompletion
{
    public class CheckCompletionConsumer : IConsumer<CheckBatchCompletionMessage>
    {
        private readonly IMediator _mediator;

        public CheckCompletionConsumer(IMediator mediator) => _mediator = mediator;

        [ExcludeFromCodeCoverage]
        public async Task Consume(ConsumeContext<CheckBatchCompletionMessage> context)
        {
            await Consume(context.Message, context.CancellationToken).ConfigureAwait(false);
        }

        public async Task Consume(CheckBatchCompletionMessage batchCompletionMessage, CancellationToken cancellationToken)
        {
            await _mediator.Send(new CheckBatchCompletion.Command(batchCompletionMessage.InboundBatchId), cancellationToken).ConfigureAwait(false);
        }
    }
}
