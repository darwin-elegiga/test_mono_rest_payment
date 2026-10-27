using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VPay.Queuing;
using VPay.Queuing.Abstractions;

namespace VPay.DocSys.Generation.Common.Rabbitmq;

public class QuorumQueueHostedService<TConsumer> : IHostedService where TConsumer : IQueueConsumer
{
    private readonly ILogger _logger;

    private readonly QueueConfigurationSettings _settings;

    private readonly IMessageReceiver _messageReceiver;

    private readonly IServiceProvider _service;

    public QuorumQueueHostedService(ILogger<TConsumer> logger, QueueConfigurationSettings queueConfigurationSettings, IServiceProvider service)
    {
        _logger = logger;
        _settings = queueConfigurationSettings;
        _messageReceiver = QuorumReceiverFactory.GetReceiver(_settings, (string value) => ProcessMessage(value, CancellationToken.None), _logger);
        _service = service;
    }

    private Task<bool> ProcessMessage(string value, CancellationToken cancellationToken)
    {
        using IServiceScope serviceScope = _service.CreateScope();
        return serviceScope.ServiceProvider.GetRequiredService<TConsumer>().Consume(value, cancellationToken);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        using (_logger.BeginScope(new Dictionary<string, object>
    {
        { "QueueName", _settings.QueueName },
        { "ErrorQueueName", _settings.ErrorQueueName },
        { "Exchange", _settings.Exchange },
        { "Host", _settings.Host },
        { "VirtualHost", _settings.VirtualHost },
        { "UserName", _settings.UserName },
        { "Port", _settings.Port },
        { "MaxPriority", _settings.MaxPriority }
    }))
        {
            _logger.LogInformation("Queue Consumer for " + typeof(TConsumer).FullName + " starting.");
        }

        _messageReceiver.Consume();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _messageReceiver.Stop();
        return Task.CompletedTask;
    }
}
