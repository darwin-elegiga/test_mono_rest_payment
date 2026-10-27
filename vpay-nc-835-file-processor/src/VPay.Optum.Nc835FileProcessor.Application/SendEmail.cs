using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using MediatR;

namespace VPay.Optum.Nc835FileProcessor.Application
{
    public static class SendEmail
    {
        public record Notification(string Message, string Subject, bool IsHtml = true) : INotification;

        public class Handler : INotificationHandler<Notification>
        {
            private readonly IPublishEndpoint _publishEndpoint;

            public Handler(IPublishEndpoint publishEndpoint) => _publishEndpoint = publishEndpoint;

            public Task Handle(Notification notification, CancellationToken cancellationToken)
            {
                return _publishEndpoint.Publish(new SendAlertEmail(notification.Message, notification.Subject, notification.IsHtml), x => x.Durable = true, cancellationToken);
            }
        }
    }
}
