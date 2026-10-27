using System.Collections.Generic;
using System.Threading.Tasks;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using VPay.Optum.Nc835FileProcessor.Application;

namespace VPay.Optum.Nc835FileProcessor.Consumer.Services
{
    public class Nc835FileNotificationConsumer : IConsumer<Nc835FileNotification>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<Nc835FileNotificationConsumer> _logger;

        public Nc835FileNotificationConsumer(
            IMediator mediator,
            ILogger<Nc835FileNotificationConsumer> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<Nc835FileNotification> context)
        {
            var message = context.Message;

            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["FilePath"] = message.FilePath,
                ["InboundFileId"] = message.InboundFileId,
            });

            await _mediator.Send(new ProcessFile.Command(message.InboundFileId, context.MessageId)).ConfigureAwait(false);
        }
    }
}
