using Microsoft.Extensions.Logging;
using VPay.Queuing;

namespace VPay.DocSys.Parsing.Common.Rabbitmq
{
    public interface IRabbitMqPublisherFactory
    {
        public IRabbitMqPublisher GetPublisher(QueueConfigurationSettings queueSettings, ILogger logger, DeliveryConfirmationMethod confirmationMethod = DeliveryConfirmationMethod.Synchronous);
    }
}
