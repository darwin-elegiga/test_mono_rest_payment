using System;
using Microsoft.Extensions.Logging;
using VPay.Queuing;

namespace VPay.DocSys.Generation.Common.Rabbitmq;

//
// Summary:
//     Currently returns an implementation for a RabbitMQ publisher. Built specifically
//     so we can return different implementations if need arises
public static class QuorumPublisherFactory
{
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
    public static IMessagePublisher GetPublisher(QueueConfigurationSettings queueSettings, ILogger logger, DeliveryConfirmationMethod confirmationMethod = DeliveryConfirmationMethod.Synchronous)
    {
        return confirmationMethod switch
        {
            DeliveryConfirmationMethod.None => new QuorumNonConfirmingPublisher(queueSettings, logger),
            DeliveryConfirmationMethod.Synchronous => new QuorumSynchronousConfirmingPublisher(queueSettings, logger),
            DeliveryConfirmationMethod.Asynchronous => new QuorumAsyncConfirmingPublisher(queueSettings, logger),
            _ => throw new NotSupportedException(string.Format("{0}.{1} is not supported.", "DeliveryConfirmationMethod", confirmationMethod)),
        };
    }

    //
    // Summary:
    //     Simple test for RabbmitMQ connectivity
    //
    // Parameters:
    //   settings:
    //     Queue Configuration settings to be tested
    //
    //   logger:
    //     Logger for connection errors
    public static bool TestConnection(QueueConfigurationSettings settings, ILogger logger)
    {
        return ConnectionTester.TestConnection(settings, logger);
    }
}
