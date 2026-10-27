using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using MediatR;
using VPay.Optum.PaymentRouting.InboundPayment.Application.Commands;

namespace VPay.Optum.PaymentRouting.InboundPayment.Consumers.Consumers
{
    public class PaymentSummaryConsumer : IConsumer<Common.Serialization.PaymentSummary>
    {
        private readonly IMediator _mediator;

        public PaymentSummaryConsumer(IMediator mediator)
        {
            _mediator = mediator;
        }

        [ExcludeFromCodeCoverage]
        public async Task Consume(ConsumeContext<Common.Serialization.PaymentSummary> context)
        {
            await Consume(context.Message, context.CancellationToken).ConfigureAwait(false);
        }

        public async Task Consume(Common.Serialization.PaymentSummary paymentSummary, CancellationToken cancellationToken)
        {
            var command = new ProcessPaymentSummary.Command { PaymentSummary = paymentSummary };
            await _mediator.Send(command, cancellationToken).ConfigureAwait(false);
        }
    }
}
