using MassTransit;
using MediatR;
using VPay.EventBus.Abstractions;
using VPay.EventBus.Messages.Transactions;
using VPay.Gateway.Common.PlatformConfig;
using VPay.Gateway.Data.Sql.Payments;

namespace VPay.Gateway.PaymentProcessor.Consumer.Rabbit;

public class TransactionChangedConsumer : IEventBusConsumer<TransactionChanged>
{
    private readonly IMediator _mediator;
    private readonly ILogger _logger;
    private readonly IBus _bus;

    public TransactionChangedConsumer(IMediator mediator, ILogger<TransactionChangedConsumer> logger, IBus bus)
    {
        _mediator = mediator;
        _logger = logger;
        _bus = bus;
    }

    public async Task Consume(ConsumeContext<TransactionChanged> context)
    {
        var config = await _mediator.Send(new ExternalClientConfigurationQuery(context.Message.ClientCode, DateTime.Now), context.CancellationToken).ConfigureAwait(false);
        if (config is null)
        {
            _logger.LogInformation("Received PaymentId = {PaymentId}.  Client '{ClientCode}' is not configured to send to Guidewire", context.Message.PaymentId, context.Message.ClientCode);
            return;
        }

        var queue = await _mediator.Send(new PaymentProcessQueueFindNextCommand(context.Message.ClientCode, context.Message.PaymentId), context.CancellationToken).ConfigureAwait(false);
        if (queue is null)
        {
            _logger.LogInformation("Received PaymentId = {PaymentId}.  Client '{ClientCode}' skipped queue", context.Message.PaymentId, context.Message.ClientCode);
            return;
        }

        //send message of transaction on
        await _bus.Publish(new NotifyClientPaymentChanged
        {
            ClientCode = context.Message.ClientCode,
            PaymentId = context.Message.PaymentId,
            RequestTime = queue.LastRequestedDateTime
        }, x => x.Durable = true, context.CancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Received PaymentId = {PaymentId}.  Client '{ClientCode}' started queue", context.Message.PaymentId, context.Message.ClientCode);
    }
}
