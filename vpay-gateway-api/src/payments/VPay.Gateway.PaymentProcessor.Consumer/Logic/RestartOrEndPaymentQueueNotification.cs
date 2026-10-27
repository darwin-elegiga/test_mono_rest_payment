using MassTransit;
using MediatR;
using VPay.Gateway.Data.Sql.Payments;
using VPay.Gateway.PaymentProcessor.Consumer.Rabbit;

namespace VPay.Gateway.PaymentProcessor.Consumer.Logic;

public class RestartOrEndPaymentQueueNotification : INotification
{
    public RestartOrEndPaymentQueueNotification(string clientCode, string clientPaymentId, DateTime requestTime)
    {
        ClientCode = clientCode;
        ClientPaymentId = clientPaymentId;
        RequestTime = requestTime;
    }

    public string ClientCode { get; }

    public string ClientPaymentId { get; }

    public DateTime RequestTime { get; }
}

public class RestartOrEndPaymentQueueNotificationHandler : INotificationHandler<RestartOrEndPaymentQueueNotification>
{
    private readonly IMediator _mediator;
    private readonly IPublishEndpoint _publishEndpoint;

    public RestartOrEndPaymentQueueNotificationHandler(IMediator mediator, IPublishEndpoint publishEndpoint)
    {
        _mediator = mediator;
        _publishEndpoint = publishEndpoint;
    }

    public async Task Handle(RestartOrEndPaymentQueueNotification notification, CancellationToken cancellationToken)
    {
        var queue = await _mediator.Send(new PaymentProcessQueueQuery(notification.ClientCode, notification.ClientPaymentId), cancellationToken).ConfigureAwait(false);

        if (queue == null)
        {
            return;
        }

        if (queue.LastRequestedDateTime == notification.RequestTime)
        {
            await _mediator.Send(new PaymentProcessQueueDeleteCommand(notification.ClientCode, notification.ClientPaymentId), cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await _publishEndpoint.Publish(new NotifyClientPaymentChanged { ClientCode = notification.ClientCode, PaymentId = notification.ClientPaymentId, RequestTime = queue.LastRequestedDateTime }, x => x.Durable = true, cancellationToken).ConfigureAwait(false);
        }
    }
}
