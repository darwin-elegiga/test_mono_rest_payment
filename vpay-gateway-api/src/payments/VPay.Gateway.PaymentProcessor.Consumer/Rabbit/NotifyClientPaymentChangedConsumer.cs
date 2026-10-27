using MassTransit;
using MediatR;
using VPay.Gateway.Common;
using VPay.Gateway.PaymentProcessor.Consumer.Logic;

namespace VPay.Gateway.PaymentProcessor.Consumer.Rabbit;

public class NotifyClientPaymentChangedConsumer : IConsumer<NotifyClientPaymentChanged>
{
    private readonly IMediator _mediator;
    private readonly ILogger<NotifyClientPaymentChangedConsumer> _logger;

    public NotifyClientPaymentChangedConsumer(IMediator mediator, ILogger<NotifyClientPaymentChangedConsumer> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<NotifyClientPaymentChanged> context)
    {
        var start = StopwatchHelpers.GetTimestamp();
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["MessageId"] = context.MessageId,
            ["ClientCode"] = context.Message.ClientCode,
            ["PaymentId"] = context.Message.PaymentId
        });

        try
        {
            var completed = await _mediator.Send(new RecordRelatedPaymentEventsCommand(context.Message.ClientCode, context.Message.PaymentId), context.CancellationToken).ConfigureAwait(false);

            if (completed)
            {
                await context.Publish(new ProcessClientPaymentStatusRecords { ClientCode = context.Message.ClientCode, PaymentId = context.Message.PaymentId, RequestTime = context.Message.RequestTime }, x => x.Durable = true, context.CancellationToken).ConfigureAwait(false);
            }
            else
            {
                await _mediator.Publish(new RestartOrEndPaymentQueueNotification(context.Message.ClientCode, context.Message.PaymentId, context.Message.RequestTime), context.CancellationToken).ConfigureAwait(false);
            }
            _logger.LogInformation("NotifyClientPaymentChanged {Status} in {Elapsed:0.0000} ms", "SUCCESS", start.GetElapsedMilliseconds());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NotifyClientPaymentChanged {Status} in {Elapsed:0.0000} ms", "FAILED", start.GetElapsedMilliseconds());
            throw;
        }
    }
}
