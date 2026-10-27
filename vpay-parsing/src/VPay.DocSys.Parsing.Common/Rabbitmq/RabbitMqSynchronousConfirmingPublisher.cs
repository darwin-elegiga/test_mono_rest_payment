using Microsoft.Extensions.Logging;
using VPay.DocSys.Common.RabbitMQ;
using VPay.Queuing;

namespace VPay.DocSys.Parsing.Common.Rabbitmq;

public class RabbitMqSynchronousConfirmingPublisher : RabbitMqPublisher
{
    public RabbitMqSynchronousConfirmingPublisher(QueueConfigurationSettings settings, ILogger logger, IMessageRepositoryFactory messageRepositoryFactory)
        : base(settings, logger, messageRepositoryFactory)
    {
        // The base constructor handles the initialization of the channel and confirms.
        // No additional setup is needed here for synchronous confirming.
    }

    // Additional methods specific to synchronous confirming can be added here if needed.
}
