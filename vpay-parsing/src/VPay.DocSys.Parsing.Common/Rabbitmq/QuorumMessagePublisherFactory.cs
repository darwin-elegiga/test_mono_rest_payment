using Microsoft.Extensions.Logging;
using VPay.Queuing;

namespace VPay.DocSys.Parsing.Common.Rabbitmq;

public class QuorumMessagePublisherFactory : IMessagePublisherFactory
{
    private readonly ILoggerFactory _loggerFactory;

    public QuorumMessagePublisherFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    //
    // Summary:
    //     Creates a publisher based on the specified settings.
    //
    // Parameters:
    //   settings:
    //     The settings for the new publisher.
    //
    // Returns:
    //     A new VPay.Queuing.IMessagePublisher instance based on the specified settings.
    public IMessagePublisher CreatePublisher(QueueConfigurationSettings settings)
    {
        return QuorumPublisherFactory.GetPublisher(settings, _loggerFactory.CreateLogger<IMessagePublisher>());
    }

    //
    // Summary:
    //     Creates a publisher based on the specified settings and logger.
    //
    // Parameters:
    //   settings:
    //     The settings for the new publisher.
    //
    //   logger:
    //     The logger for the publisher to use.
    //
    // Returns:
    //     A new VPay.Queuing.IMessagePublisher instance based on the specified settings
    //     and logger.
    public IMessagePublisher CreatePublisher(QueueConfigurationSettings settings, ILogger logger)
    {
        return QuorumPublisherFactory.GetPublisher(settings, logger);
    }
}
