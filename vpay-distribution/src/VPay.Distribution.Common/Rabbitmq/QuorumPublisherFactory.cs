using System;
using Microsoft.Extensions.Logging;
using VPay.Queuing;

namespace FaxManagement.Core.Rabbitmq
{

    //
    // Summary:
    //     Currently returns an implementation for a RabbitMQ publisher. Built specifically
    //     so we can return different implementations if need arises
    public static class QuorumPublisherFactory
    {
        public static IMessagePublisher GetPublisher(QueueConfigurationSettings queueSettings, ILogger logger, DeliveryConfirmationMethod confirmationMethod = DeliveryConfirmationMethod.Synchronous)
        {
            switch (confirmationMethod)
            {
                case DeliveryConfirmationMethod.None:
                    return new QuorumNonConfirmingPublisher(queueSettings, logger);
                case DeliveryConfirmationMethod.Synchronous:
                    return new QuorumSynchronousConfirmingPublisher(queueSettings, logger);
                case DeliveryConfirmationMethod.Asynchronous:
                    return new QuorumAsyncConfirmingPublisher(queueSettings, logger);
                default:
                    throw new NotSupportedException(string.Format("{0}.{1} is not supported.", "DeliveryConfirmationMethod", confirmationMethod));
            }
        }

        public static bool TestConnection(QueueConfigurationSettings settings, ILogger logger)
        {
            return ConnectionTester.TestConnection(settings, logger);
        }
    }
}
