using System.Collections.Generic;
using System.Threading.Tasks;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using VPay.Optum.Nc835Record.Application;

namespace VPay.Optum.Nc835Record.Consumer.Services
{
    public class Nc835RecordNotificationConsumer : IConsumer<Nc835RecordNotification>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<Nc835RecordNotificationConsumer> _logger;

        public Nc835RecordNotificationConsumer(
            IMediator mediator,
            ILogger<Nc835RecordNotificationConsumer> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<Nc835RecordNotification> context)
        {
            var message = context.Message;

            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["InboundFileRecordId"] = message.InboundFileRecordId,
                ["TransactionId"] = message.TransactionId
            });

            _logger.LogInformation("Processing record {InboundFileRecordId} for transaction {TransactionId}", message.InboundFileRecordId, message.TransactionId);
            await _mediator.Send(new ProcessIndividualRecord.Command(message, context.MessageId)).ConfigureAwait(false);
        }
    }
}
