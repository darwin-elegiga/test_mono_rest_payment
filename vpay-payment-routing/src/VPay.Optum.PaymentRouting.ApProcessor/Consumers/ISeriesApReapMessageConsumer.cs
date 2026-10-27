using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using VPay.Optum.PaymentRouting.Common.RabbitMessages;

namespace VPay.Optum.PaymentRouting.ApProcessor.Consumers
{
    public class ISeriesApReapMessageConsumer : IConsumer<ApReapMessage>
    {
        private readonly ILogger<ISeriesApReapMessageConsumer> _logger;
        private readonly IMediator _mediator;

        public ISeriesApReapMessageConsumer(ILogger<ISeriesApReapMessageConsumer> logger, IMediator mediator)
        {
            _logger = logger;
            _mediator = mediator;
        }

        [ExcludeFromCodeCoverage]
        public Task Consume(ConsumeContext<ApReapMessage> context)
        {
            ApReapMessage message = context.Message;
            return Consume(message);
        }

        internal async Task Consume(ApReapMessage message)
        {
            _logger.LogInformation("Received iSeries message for payment {UserKey}", message.UserKey);
            await _mediator.Send(new ConsumeApMessage.Command(message));
        }
    }
}
