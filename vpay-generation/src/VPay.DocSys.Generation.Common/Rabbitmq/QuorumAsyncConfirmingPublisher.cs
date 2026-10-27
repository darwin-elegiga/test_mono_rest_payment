using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using VPay.Queuing;

namespace VPay.DocSys.Generation.Common.Rabbitmq;

//
// Summary:
//     RabbitMQ implementation of IMessagePublisher Uses durable queues Publisher confirms
//     for guaranteed message persistence Uses callbacks to confirm publish, NACK messages
//     are published to an error queue.
public class QuorumAsyncConfirmingPublisher : IMessagePublisher, IDisposable
{
    protected class PriorityMessage
    {
        public string Body { get; set; }

        public byte? Priority { get; set; }
    }

    //
    // Summary:
    //     Only one connection to RabbitMQ for this particular thread.
    private IConnection _connection;

    //
    // Summary:
    //     Main publishing channel. This is where the publishes will be pushed in real-time
    //     and callbacks will handle ACK/NACK
    private IModel _mainChannel;

    //
    // Summary:
    //     Thread-safe container to keep all of the messages we initially publish on the
    //     main channel until they are ACK'ed or NACK'ed
    private ConcurrentDictionary<ulong, PriorityMessage> _outstandingConfirms = new ConcurrentDictionary<ulong, PriorityMessage>();

    //
    // Summary:
    //     Error publishing channel. In the case of NACK message, this channel will publish
    //     messages to an error queue.
    private IModel _errorChannel;

    public QueueConfigurationSettings Settings { get; set; }

    public string QueueName
    {
        get
        {
            return Settings.QueueName;
        }
        set
        {
            Settings.QueueName = value;
        }
    }

    public string Exchange
    {
        get
        {
            return Settings.Exchange;
        }
        set
        {
            Settings.Exchange = value;
        }
    }

    public string Host
    {
        get
        {
            return Settings.Host;
        }
        set
        {
            Settings.Host = value;
        }
    }

    public string VirtualHost
    {
        get
        {
            return Settings.VirtualHost;
        }
        set
        {
            Settings.VirtualHost = value;
        }
    }

    public int Port
    {
        get
        {
            return Settings.Port;
        }
        set
        {
            Settings.Port = value;
        }
    }

    public string Application
    {
        get
        {
            return Settings.Application;
        }
        set
        {
            Settings.Application = value;
        }
    }

    public ILogger Logger { get; set; }

    public QuorumAsyncConfirmingPublisher(QueueConfigurationSettings settings, ILogger logger)
    {
        Settings = settings;
        Logger = logger;
        Dictionary<string, object> dictionary = new Dictionary<string, object>();
        dictionary.Add("x-queue-type", "quorum");

        _connection = RetryingConnectionFactory.GetConnection(Settings.UserName, Settings.Password, Port, VirtualHost, Host, Settings.Heartbeat);
        InitializeMainChannel(settings, dictionary);
    }

    //
    // Summary:
    //     Set up the main channel to do real-time publishing and register the callbacks
    private void InitializeMainChannel(QueueConfigurationSettings settings, Dictionary<string, object> queueArgs)
    {
        _mainChannel = _connection.CreateModel();
        if (!string.IsNullOrEmpty(settings.Exchange) && !string.IsNullOrEmpty(settings.ExchangeType))
        {
            _mainChannel.ExchangeDeclare(settings.Exchange, settings.ExchangeType);
        }

        if (!string.IsNullOrEmpty(QueueName) && string.IsNullOrEmpty(Exchange))
        {
            _mainChannel.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false, queueArgs);
        }

        _mainChannel.ConfirmSelect();
        _mainChannel.BasicAcks += _mainChannel_BasicAcks;
        _mainChannel.BasicNacks += _mainChannel_BasicNacks;
    }

    //
    // Summary:
    //     Set up the error channel to publish any NACK'ed messages.
    private bool InitializeErrorChannel(QueueConfigurationSettings settings)
    {
        if (_errorChannel == null && !string.IsNullOrWhiteSpace(settings.QueueName))
        {
            _errorChannel = _connection.CreateModel();
            _errorChannel.QueueDeclare(settings.QueueName + "_PublishError", durable: true, exclusive: false, autoDelete: false, null);
            _errorChannel.ConfirmSelect();
        }

        return _errorChannel != null;
    }

    //
    // Summary:
    //     Publish to the queue and return immediately. Confirms are callbacks and handled
    //     asynchronously.
    public void Publish(string message, byte? priority = 0)
    {
        Publish(new PriorityMessage
        {
            Body = message,
            Priority = priority
        });
    }

    //
    // Summary:
    //     Publish to the queue and return immediately. Confirms are callbacks and handled
    //     asynchronously.
    protected void Publish(PriorityMessage priorityMessage)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(priorityMessage.Body);
        try
        {
            IBasicProperties basicProperties = _mainChannel.CreateBasicProperties();
            basicProperties.DeliveryMode = 2;
            if (priorityMessage.Priority.HasValue)
            {
                basicProperties.Priority = priorityMessage.Priority.Value;
            }

            if (!string.IsNullOrEmpty(Application))
            {
                basicProperties.AppId = Application;
            }

            try
            {
                _outstandingConfirms.TryAdd(_mainChannel.NextPublishSeqNo, priorityMessage);
                _mainChannel.BasicPublish(Exchange ?? string.Empty, QueueName, basicProperties, bytes);
            }
            catch (AlreadyClosedException)
            {
                _mainChannel.WaitForAutoRecovery(_connection.Heartbeat);
                _mainChannel.BasicPublish(Exchange ?? string.Empty, QueueName, basicProperties, bytes);
            }

            Logger.LogDebug((QueueName ?? Exchange) + ": Message Queued");
        }
        catch (Exception ex2)
        {
            Logger.LogError(ex2, (QueueName ?? Exchange) + ": " + ex2.Message);
            throw;
        }
    }

    //
    // Summary:
    //     Publishes to error queue only on NACK.
    private void PublishError(PriorityMessage priorityMessage)
    {
        if (!InitializeErrorChannel(Settings))
        {
            return;
        }

        using (new PerfTimerLogger(Logger, "ErrorChannel_Publish"))
        {
            byte[] bytes = Encoding.UTF8.GetBytes(priorityMessage.Body);
            try
            {
                IBasicProperties basicProperties = _errorChannel.CreateBasicProperties();
                basicProperties.DeliveryMode = 2;
                if (!string.IsNullOrEmpty(Application))
                {
                    basicProperties.AppId = Application;
                }

                try
                {
                    _errorChannel.BasicPublish(Settings.Exchange ?? string.Empty, Settings.ErrorQueueName, basicProperties, bytes);
                    _errorChannel.WaitForConfirmsOrDie(TimeSpan.FromMilliseconds(5000.0));
                }
                catch (AlreadyClosedException)
                {
                    _errorChannel.WaitForAutoRecovery(_connection.Heartbeat);
                    _errorChannel.BasicPublish(Settings.Exchange ?? string.Empty, Settings.ErrorQueueName, basicProperties, bytes);
                    _errorChannel.WaitForConfirmsOrDie(TimeSpan.FromMilliseconds(5000.0));
                }

                Logger.LogDebug((Settings.ErrorQueueName ?? Exchange) + ": Message Queued");
            }
            catch (Exception ex2)
            {
                Logger.LogError(ex2, (QueueName ?? Settings.Exchange) + ": " + ex2.Message);
            }
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
    //     Deletes the queue associated with this publisher.
    //
    // Returns:
    //     A System.UInt32 value indicating the number of messages purged during queue deletion.
    public uint DeleteQueue()
    {
        if (_mainChannel == null || _mainChannel.IsClosed)
        {
            throw new AlreadyClosedException(new ShutdownEventArgs(ShutdownInitiator.Application, 200, "Goodbye"));
        }

        return _mainChannel.QueueDelete(Settings.QueueName, ifUnused: true, ifEmpty: true);
    }

    //
    // Summary:
    //     Deletes the error queue associated with this publisher.
    //
    // Returns:
    //     A System.UInt32 value indicating the number of messages purged during queue deletion.
    public uint DeleteErrorQueue()
    {
        if (_errorChannel == null || _errorChannel.IsClosed)
        {
            throw new AlreadyClosedException(new ShutdownEventArgs(ShutdownInitiator.Application, 200, "Goodbye"));
        }

        return _errorChannel.QueueDelete(Settings.ErrorQueueName, ifUnused: true, ifEmpty: true);
    }

    //
    // Summary:
    //     Validates that the message was successfully published.
    protected virtual void _mainChannel_BasicAcks(object sender, BasicAckEventArgs e)
    {
        string message = $"Message Ack'ed - DeliveryTag: {e.DeliveryTag} - Multiple: {e.Multiple}";
        Logger.LogDebug(message);
        CleanOutstandingConfirms(e.DeliveryTag, e.Multiple);
    }

    //
    // Summary:
    //     Indicates that the initial publish on the main channel did not successfully publish.
    //     It will then be put into the Nack'ed Messages queue to be published.
    protected virtual void _mainChannel_BasicNacks(object sender, BasicNackEventArgs e)
    {
        string message = $"NACK Message received - DeliveryTag: {e.DeliveryTag} - Multiple: {e.Multiple} - Requeue: {e.Requeue}";
        Logger.LogDebug(message);
        if (!_outstandingConfirms.TryGetValue(e.DeliveryTag, out var value))
        {
            using (Logger.BeginScope(new Dictionary<string, object>
        {
            { "DeliveryTag", e.DeliveryTag },
            {
                "Body",
                value?.Body ?? "null"
            }
        }))
            {
                Logger.LogInformation("Unable to find key for DeliveryTag" + e.DeliveryTag);
            }
        }

        PublishError(value);
    }

    //
    // Summary:
    //     For a given sequenceNumber, remove from our internal list of messages needing
    //     to be confirmed.
    //
    // Parameters:
    //   sequenceNumber:
    //     The sequence number in the main channel of the message to clean.
    //
    //   multiple:
    //     Confirm all up to the point in the latest sequence.
    private void CleanOutstandingConfirms(ulong sequenceNumber, bool multiple)
    {
        PriorityMessage value;
        if (multiple)
        {
            foreach (KeyValuePair<ulong, PriorityMessage> item in _outstandingConfirms.Where((KeyValuePair<ulong, PriorityMessage> k) => k.Key <= sequenceNumber))
            {
                _outstandingConfirms.TryRemove(item.Key, out value);
            }

            return;
        }

        _outstandingConfirms.TryRemove(sequenceNumber, out value);
    }

    //
    // Summary:
    //     Cleanup channel and connection
    public void Dispose()
    {
        if (!_mainChannel.IsClosed)
        {
            _mainChannel.Close();
        }

        if (_connection.IsOpen)
        {
            _connection.Close();
        }

        _mainChannel.Dispose();
        _connection.Dispose();
        if (_errorChannel != null)
        {
            if (!_errorChannel.IsClosed)
            {
                _errorChannel.Close();
            }

            _errorChannel.Dispose();
        }

        _mainChannel = null;
        _errorChannel = null;
        _connection = null;
    }
}
