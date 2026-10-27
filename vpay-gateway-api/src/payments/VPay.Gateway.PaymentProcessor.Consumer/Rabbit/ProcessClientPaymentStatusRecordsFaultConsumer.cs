using MassTransit;
using VPay.Gateway.Common;
using VPay.Gateway.MessageBus.RabbitMQ;

namespace VPay.Gateway.PaymentProcessor.Consumer.Rabbit;

public class ProcessClientPaymentStatusRecordsFaultConsumer : IConsumer<Fault<ProcessClientPaymentStatusRecords>>
{
    private readonly ILogger<ProcessClientPaymentStatusRecordsFaultConsumer> _logger;

    public ProcessClientPaymentStatusRecordsFaultConsumer(ILogger<ProcessClientPaymentStatusRecordsFaultConsumer> logger) =>
        _logger = logger;

    public Task Consume(ConsumeContext<Fault<ProcessClientPaymentStatusRecords>> context)
    {
        var originalMessage = context.Message.Message;

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["MessageId"] = context.Message.FaultedMessageId,
            ["ClientCode"] = originalMessage.ClientCode,
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
