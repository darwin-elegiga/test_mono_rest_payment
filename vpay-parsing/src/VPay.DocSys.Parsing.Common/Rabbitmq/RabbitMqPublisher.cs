using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using VPay.DocSys.Common.RabbitMQ;
using VPay.Queuing;

namespace VPay.DocSys.Parsing.Common.Rabbitmq;

public abstract class RabbitMqPublisher : IRabbitMqPublisher, IDisposable
{
    private ILogger _logger;
    private IConnection _connection;
    private IModel _channel;
    private string _queueName;
    private string _exchange;
    private string _host;
    private string _virtualHost;
    private int _port;
    private string _application;
    private string _password;
    private string _userName;

    private readonly IMessageRepositoryFactory _messageRepositoryFactory;

    public RabbitMqPublisher(QueueConfigurationSettings settings, ILogger logger, IMessageRepositoryFactory messageRepositoryFactory)
    {
        _application = settings.Application;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        _logger = logger;
        _queueName = settings.QueueName;
        _exchange = settings.Exchange;
        _host = settings.Host;
        _virtualHost = settings.VirtualHost;
        _port = settings.Port;
        _userName = settings.UserName;
        _password = settings.Password;

        Dictionary<string, object> dictionary = new Dictionary<string, object>();
        dictionary.Add("x-queue-type", "quorum");

        if (settings is IConsumerTimeoutQueueConfiguration consumerTimeoutConfig)
        {
            if (consumerTimeoutConfig.ConsumerTimeout > 0)
            {
                dictionary.Add("x-consumer-timeout", consumerTimeoutConfig.ConsumerTimeout);
            }
        }

        _connection = RetryingConnectionFactory.GetConnection(_userName, _password, _port, _virtualHost, _host, settings.Heartbeat);
        _channel = _connection.CreateModel();
        if (!string.IsNullOrEmpty(settings.Exchange) && !string.IsNullOrEmpty(settings.ExchangeType))
        {
            _channel.ExchangeDeclare(settings.Exchange, settings.ExchangeType);
        }

        if (!string.IsNullOrEmpty(_queueName) && string.IsNullOrEmpty(_exchange))
        {
            _channel.QueueDeclare(_queueName, durable: true, exclusive: false, autoDelete: false, dictionary);
        }

        _channel.ConfirmSelect();

        _messageRepositoryFactory = messageRepositoryFactory;
    }

    public void Publish(string message)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(message);
        try
        {
            IBasicProperties basicProperties = _channel.CreateBasicProperties();
            basicProperties.DeliveryMode = 2;

            if (!string.IsNullOrEmpty(_application))
            {
                basicProperties.AppId = _application;
            }

            try
            {
                _channel.BasicPublish(_exchange ?? string.Empty, _queueName, basicProperties, bytes);
            }
            catch (AlreadyClosedException)
            {
                _channel.WaitForAutoRecovery(_connection.Heartbeat);
                _channel.BasicPublish(_exchange ?? string.Empty, _queueName, basicProperties, bytes);
            }

            try
            {
                _channel.WaitForConfirmsOrDie();
            }
            catch (AlreadyClosedException)
            {
                _channel.WaitForAutoRecovery(_connection.Heartbeat);
                _channel.WaitForConfirmsOrDie();
            }

            _logger.LogDebug((_queueName ?? _exchange) + ": Message Queued");
        }
        catch (Exception ex3)
        {
            _logger.LogError(ex3, (_queueName ?? _exchange) + ": " + ex3.Message);
            throw;
        }
    }

    public void PublishUsingMessageRepository<TMessage>(TMessage message) where TMessage : class
    {
        IMessageRepository<TMessage> repository = _messageRepositoryFactory.CreateMessageRepository<TMessage>();

        if (repository == null)
        {
            throw new InvalidOperationException($"No message repository found for type {typeof(TMessage).FullName}");
        }

        var messageData = repository.Put(message);
        if (messageData != null)
        {
            Publish(JsonSerializer.Serialize(messageData));
        }
        else
        {
            _logger.LogWarning("Message data is null, nothing to publish.");
        }
    }

    public bool IsConnected()
    {
        return _connection.IsOpen;
    }

    public uint DeleteQueue()
    {
        if (_channel == null || _channel.IsClosed)
        {
            throw new AlreadyClosedException(new ShutdownEventArgs(ShutdownInitiator.Application, 200, "Goodbye"));
        }

        return _channel.QueueDelete(_queueName, ifUnused: true, ifEmpty: true);
    }

    public void Dispose()
    {
        if (!_channel.IsClosed)
        {
            _channel.Close();
        }

        if (_connection.IsOpen)
        {
            _connection.Close();
        }

        _channel.Dispose();
        _connection.Dispose();
        _channel = null;
        _connection = null;        // Default implementation does nothing
    }
}
