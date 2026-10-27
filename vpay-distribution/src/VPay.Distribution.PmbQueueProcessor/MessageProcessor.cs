using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VPay.Distribution.Common.Rabbitmq;
using VPay.Distribution.PmbQueueProcessor.Services;
using VPay.Queuing;

namespace VPay.Distribution.PmbQueueProcessor;

public class MessageProcessor
{
    private readonly IServiceProvider _services;
    private IMessageReceiver _queueReceiver;
    private ILogger _queueLogger;

    public MessageProcessor(IServiceProvider services) => _services = services;

    public void Start(QueueConfigurationSettings settings, ILogger queueLogger)
    {
        _queueLogger = queueLogger ?? throw new ArgumentNullException(nameof(queueLogger));

        using (queueLogger.BeginScope(new Dictionary<string, object>()
            {
                { "QueueName", settings.QueueName },
                { "ErrorQueueName", settings.ErrorQueueName },
                { "Exchange", settings.Exchange },
                { "Host", settings.Host },
                { "VirtualHost", settings.VirtualHost },
                { "UserName", settings.UserName },
                { "Port", settings.Port },
                { "MaxPriority", settings.MaxPriority },
            }))
        {
            queueLogger.LogInformation($"{typeof(MessageProcessor).FullName} starting.");
        }

        _queueReceiver = QuorumReceiverFactory.GetReceiver(settings, (string s) => ProcessMessage(s, CancellationToken.None), queueLogger);

        _queueReceiver.Consume();
    }

    public void Stop()
    {
        _queueReceiver.Stop();
    }

    private Task<bool> ProcessMessage(string message, CancellationToken cancellationToken)
    {
        if (!message.StartsWith("PMB"))
        {
            _queueLogger.LogError("Invalid message format: {Message}", message);
            throw new ArgumentException("Invalid message format", nameof(message));
        }

        using (var scope = _services.CreateScope())
        {
            var scopedProcessingService =
                scope.ServiceProvider
                    .GetRequiredService<IAs400DataQueueService>();

            scopedProcessingService.WriteToQueue(message);
        }

        return Task.FromResult(true);
    }
}
