using MassTransit;
using VPay.Gateway.Common;
using VPay.Gateway.Common.Commands;
using VPay.Gateway.MessageBus.RabbitMQ;

namespace VPay.Gateway.ClientProcessor.Rabbit;

public class SendGuidewireMessageFaultConsumer : IConsumer<Fault<SendGuidewireMessage>>
{
    private readonly ILogger<SendGuidewireMessageFaultConsumer> _logger;

    public SendGuidewireMessageFaultConsumer(ILogger<SendGuidewireMessageFaultConsumer> logger) =>
        _logger = logger;

    public Task Consume(ConsumeContext<Fault<SendGuidewireMessage>> context)
    {
        var originalMessage = context.Message.Message;

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["MessageId"] = context.Message.FaultedMessageId,
            ["ClientCode"] = originalMessage.ClientCode,
            ["ClaimNumber"] = originalMessage.ClaimNumber,
            ["PayeeId"] = originalMessage.PayeeId,
            ["PaymentId"] = originalMessage.PaymentId
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
