using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using VPay.Queuing;

namespace VPay.DocSys.Generation.Common.Rabbitmq;

//
// Summary:
//     RabbitMQ implementation of IMessagePublisher Uses durable queues Publisher does
//     not confirm message delivery
public class QuorumNonConfirmingPublisher : IMessagePublisher, IDisposable
{
    private IConnection _connection;

    private IModel _channel;

    private string _password;

    private string _userName;

    public string QueueName { get; set; }

    public string Exchange { get; set; }

    public string Host { get; set; }

    public string VirtualHost { get; set; }

    public int Port { get; set; }

    public string Application { get; set; }

    public ILogger Logger { get; set; }

    public QuorumNonConfirmingPublisher(QueueConfigurationSettings settings, ILogger logger)
    {
        Logger = logger;
        QueueName = settings.QueueName;
        Exchange = settings.Exchange;
        Host = settings.Host;
        VirtualHost = settings.VirtualHost;
        Port = settings.Port;
        _password = settings.Password;
        _userName = settings.UserName;
        Application = settings.Application;

        Dictionary<string, object> dictionary = new Dictionary<string, object>();
        dictionary.Add("x-queue-type", "quorum");

        _connection = RetryingConnectionFactory.GetConnection(_userName, _password, Port, VirtualHost, Host, settings.Heartbeat);
        _channel = _connection.CreateModel();
        if (!string.IsNullOrEmpty(settings.Exchange) && !string.IsNullOrEmpty(settings.ExchangeType))
        {
            _channel.ExchangeDeclare(settings.Exchange, settings.ExchangeType);
        }

        if (!string.IsNullOrEmpty(QueueName) && string.IsNullOrEmpty(Exchange))
        {
            _channel.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false, dictionary);
        }
    }

    //
    // Summary:
    //     Send the message to the queue
    //
    // Parameters:
    //   message:
    //     Message content
    //
    //   priority:
    //     Priority of message
    public void Publish(string message, byte? priority = 0)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(message);
        try
        {
            IBasicProperties basicProperties = _channel.CreateBasicProperties();
            basicProperties.DeliveryMode = 2;
            if (priority.HasValue)
            {
                basicProperties.Priority = priority.Value;
            }

            if (!string.IsNullOrEmpty(Application))
            {
                basicProperties.AppId = Application;
            }

            try
            {
                _channel.BasicPublish(Exchange ?? string.Empty, QueueName, basicProperties, bytes);
            }
            catch (AlreadyClosedException)
            {
                _channel.WaitForAutoRecovery(_connection.Heartbeat);
                _channel.BasicPublish(Exchange ?? string.Empty, QueueName, basicProperties, bytes);
            }

            Logger.LogDebug((QueueName ?? Exchange) + ": Message queued without confirmation");
        }
        catch (Exception ex2)
        {
            Logger.LogError(ex2, (QueueName ?? Exchange) + ": " + ex2.Message);
            throw;
        }
    }

    //
    // Summary:
    //     Connection test for health check
    //
    // Returns:
    //     Is the connection open
    public bool IsConnected()
    {
        return _connection.IsOpen;
    }

    //
    // Summary:
    //     Cleanup channel and connection
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
        _connection = null;
    }

    //
    // Summary:
    //     Deletes the queue associated with this publisher.
    //
    // Returns:
    //     A System.UInt32 value indicating the number of messages purged during queue deletion.
    public uint DeleteQueue()
    {
        if (_channel == null || _channel.IsClosed)
        {
            throw new AlreadyClosedException(new ShutdownEventArgs(ShutdownInitiator.Application, 200, "Goodbye"));
        }

        return _channel.QueueDelete(QueueName, ifUnused: true, ifEmpty: true);
    }
}
