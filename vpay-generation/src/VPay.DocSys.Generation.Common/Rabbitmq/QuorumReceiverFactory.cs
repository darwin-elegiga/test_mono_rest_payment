using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client.Events;
using VPay.Queuing;

namespace VPay.DocSys.Generation.Common.Rabbitmq
{
    public static class QuorumReceiverFactory
    {
        //
        // Summary:
        //     Factory method for returning a an instance of a MessageReceiver implementation
        //
        //
        // Parameters:
        //   settings:
        //     An object containing connection parameters
        //
        //   callback:
        //     Method for processing message
        //
        //   logger:
        //     logging provider
        //
        //   beforeHandleDequeueError:
        //     An optional delegate that will be called when an error occurs while dequeuing
        //     a message. The delegate should return false to prevent the error from being logged
        //     and the message from being published to the error queue; otherwise, the delegate
        //     should return true.
        //
        //   afterHandleDequeueError:
        //     An optional delegate that will be called after an error occurs while dequeuing
        //     a message. The delegate is called after the error has been logged and the message
        //     has been published to the error queue.
        //
        // Returns:
        //     Instance of IMessageRepositorytReceiver currently only one RabbitMQ-based implementation
        public static IMessageReceiver GetReceiver(
            QueueConfigurationSettings settings,
            Func<string, Task<bool>> callback,
            ILogger logger,
            Func<string, Exception, bool> beforeHandleDequeueError = null,
            Action<string, Exception> afterHandleDequeueError = null)
        {
            return new QuorumRabbitMQReceiver(settings, callback, logger, beforeHandleDequeueError, afterHandleDequeueError);
        }

        //
        // Summary:
        //     Factory method for returning a an instance of a MessageReceiver implementation
        //
        //
        // Parameters:
        //   settings:
        //     An object containing connection parameters
        //
        //   callback:
        //     Method for processing message
        //
        //   logger:
        //     logging provider
        //
        //   beforeHandleDequeueError:
        //     An optional delegate that will be called when an error occurs while dequeuing
        //     a message. The delegate should return false to prevent the error from being logged
        //     and the message from being published to the error queue; otherwise, the delegate
        //     should return true.
        //
        //   afterHandleDequeueError:
        //     An optional delegate that will be called after an error occurs while dequeuing
        //     a message. The delegate is called after the error has been logged and the message
        //     has been published to the error queue.
        //
        // Returns:
        //     Instance of IMessageReceiver currently only one RabbitMQ-based implementation
        public static IMessageReceiver GetReceiver(
            QueueConfigurationSettings settings,
            Func<BasicDeliverEventArgs,
            Task<bool>> callback,
            ILogger logger,
            Func<BasicDeliverEventArgs, Exception, bool> beforeHandleDequeueError = null,
            Action<BasicDeliverEventArgs, Exception> afterHandleDequeueError = null)
        {
            return new QuorumRabbitMQReceiver(settings, callback, logger, beforeHandleDequeueError, afterHandleDequeueError);
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
}
