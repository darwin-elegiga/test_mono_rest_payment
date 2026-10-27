using MassTransit;
using VPay.Gateway.Common;
using VPay.Gateway.DocumentsProcessor.Abstractions;
using VPay.Gateway.MessageBus.RabbitMQ;

namespace VPay.Gateway.DocumentsProcessor.Consumer.Rabbit.Guidewire;

public class GuidewireDocumentNotificationFaultConsumer : IConsumer<Fault<GuidewireDocumentNotification>>
{
    private readonly ILogger<GuidewireDocumentNotificationFaultConsumer> _logger;

    public GuidewireDocumentNotificationFaultConsumer(ILogger<GuidewireDocumentNotificationFaultConsumer> logger) =>
        _logger = logger;

    public Task Consume(ConsumeContext<Fault<GuidewireDocumentNotification>> context)
    {
        var originalMessage = context.Message.Message;

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["MessageId"] = context.Message.FaultedMessageId,
            ["ArchiveDetailId"] = originalMessage.DocumentId,
            ["TransactionId"] = originalMessage.TransactionId
        });

        foreach (var ex in context.Message.Exceptions)
        {
            using var innerScope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["ErrorDetail"] = ex.GetString().Truncate(5_000, ellipsis: true)
            });

            _logger.LogCritical("Consumer faulted");
        }

        return Task.CompletedTask;
    }
}
