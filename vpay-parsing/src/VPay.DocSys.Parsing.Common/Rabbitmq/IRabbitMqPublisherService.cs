using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VPay.Queuing;

namespace VPay.DocSys.Parsing.Common.Rabbitmq;

public interface IRabbitMqPublisherService
{
    //
    // Summary:
    //     Publishes the specified message to the queue specified in queueSettings.
    //
    // Parameters:
    //   queueSettings:
    //     An object that contains information about the host and queue to publish the message
    //     to.
    //
    //   message:
    //     The message to publish.
    void PublishMessage(QueueConfigurationSettings queueSettings, string message);

    //
    // Summary:
    //     Publishes the specified message to the queue specified in queueSettings with
    //     the specified priority.
    //
    // Parameters:
    //   queueSettings:
    //     An object that contains information about the host and queue to publish the message
    //     to.
    //
    //   message:
    //     The message to publish.
    //
    //   priority:
    //     The priority of the message.
    void PublishMessageUsingMessageRepository<T>(QueueConfigurationSettings queueSettings, T message);
}
