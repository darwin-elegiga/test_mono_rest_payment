using VPay.Queuing;

namespace VPay.DocSys.Parsing.Common.Rabbitmq;

internal interface IRabbitMqPublisherCache
{
    //
    // Summary:
    //     Gets the publisher associated with the specified queueConfigurationSettings creating
    //     an instance if one does not already exist.
    //
    // Parameters:
    //   queueConfigurationSettings:
    //     The settings of the publisher to find.
    //
    // Returns:
    //     The VPay.Queuing.IMessagePublisher instance associated with the specified queueConfigurationSettings.
    IMessagePublisher GetOrAddPublisher(QueueConfigurationSettings queueConfigurationSettings);
}
