using System.Threading.Tasks;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using VPay.Optum.PaymentRouting.Common.RabbitMessages;
using VPay.Optum.PaymentRouting.OutboundProcessor.Application.Commands;

namespace VPay.Optum.PaymentRouting.OutboundProcessor.Consumers.Outbound835File
{
    public class Outbound835FileConsumer : IConsumer<Outbound835FileMessage>
    {
        private readonly ILogger<Outbound835FileConsumer> _logger;
        private readonly IMediator _mediator;

        public Outbound835FileConsumer(ILogger<Outbound835FileConsumer> logger, IMediator mediator)
        {
            _logger = logger;
            _mediator = mediator;
        }

        public async Task Consume(ConsumeContext<Outbound835FileMessage> context)
        {
            _logger.LogInformation("Received message to create Outbound835 file.");
            await _mediator.Send(new CreateOutbound835File.Command(context.Message.Id));
        }
    }
}
