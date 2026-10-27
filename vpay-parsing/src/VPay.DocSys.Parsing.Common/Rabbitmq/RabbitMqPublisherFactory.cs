using System;
using Microsoft.Extensions.Logging;
using VPay.DocSys.Common.RabbitMQ;
using VPay.Queuing;

namespace VPay.DocSys.Parsing.Common.Rabbitmq;

public class RabbitMqPublisherFactory : IRabbitMqPublisherFactory
{
    private readonly IMessageRepositoryFactory _messageRepositoryFactory;

    public RabbitMqPublisherFactory(IMessageRepositoryFactory messageRepositoryFactory) => _messageRepositoryFactory = messageRepositoryFactory;

    //
    // Summary:
    //     Gets an instance of an object that can be used for publishing messages to a message
    //     queue.
    //
    // Parameters:
    //   queueSettings:
    //     The connection and configuration settings for the to which the VPay.Queuing.IMessagePublisher
    //     will publish.
    //
    //   logger:
    //     The logger to use.
    //
    //   confirmationMethod:
    //     A VPay.Queuing.DeliveryConfirmationMethod value indicating the method the MessagePublisher
    //     will use for message delivery confirmations.
    //
    // Returns:
    //     The an instance of VPay.Queuing.IMessagePublisher which uses the specified confirmationMethod
    //     for message delivery confirmations.
    public IRabbitMqPublisher GetPublisher(QueueConfigurationSettings queueSettings, ILogger logger, DeliveryConfirmationMethod confirmationMethod = DeliveryConfirmationMethod.Synchronous)
    {
        return confirmationMethod switch
        {
            //DeliveryConfirmationMethod.None => new QuorumNonConfirmingPublisher(queueSettings, logger),
            DeliveryConfirmationMethod.Synchronous => new RabbitMqSynchronousConfirmingPublisher(queueSettings, logger, _messageRepositoryFactory),
            //DeliveryConfirmationMethod.Asynchronous => new QuorumAsyncConfirmingPublisher(queueSettings, logger),
            _ => throw new NotSupportedException(string.Format("{0}.{1} is not supported.", "DeliveryConfirmationMethod", confirmationMethod)),
        };
    }
}
