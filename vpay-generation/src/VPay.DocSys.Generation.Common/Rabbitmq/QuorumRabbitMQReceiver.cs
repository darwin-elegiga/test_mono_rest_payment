using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using VPay.DocSys.Common.RabbitMQ;
using VPay.Queuing;

namespace VPay.DocSys.Generation.Common.Rabbitmq;

public class QuorumRabbitMQReceiver : IMessageRepositoryReceiver, IDisposable
{
    private IConnection _connection;

    public IModel Channel;

    private string _password;

    private string _userName;

    private EventingBasicConsumer _consumer;

    private IModel _errorChannel;

    private bool _useErrorQueue;

    private bool _autoAck;

    private int _delayBetweenMessages;

    private Func<string, Exception, bool> _beforeHandleDequeueErrorFunc;

    private Action<string, Exception> _afterHandleDequeueErrorFunc;

    private Func<string, Task<bool>> _messageTargetFunc;

    private EventHandler<BasicDeliverEventArgs> _messageTargetDelegate;

    private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions { WriteIndented = true };

    public string QueueName { get; set; }

    public string ErrorQueueName { get; set; }

    public string Host { get; set; }

    public string VirtualHost { get; set; }

    public int Port { get; set; }

    public ILogger Logger { get; set; }

    public QuorumRabbitMQReceiver(
        QueueConfigurationSettings settings,
        Func<BasicDeliverEventArgs,
        Task<bool>> messageTarget,
        ILogger logger,
        Func<BasicDeliverEventArgs, Exception, bool> beforeHandleDequeueError = null,
        Action<BasicDeliverEventArgs, Exception> afterHandleDequeueError = null)
    {
        QuorumRabbitMQReceiver rabbitMQReceiver = this;
        SetupReceiver(settings, logger);

        _messageTargetDelegate = delegate (object model, BasicDeliverEventArgs ea)
        {
            try
            {
                Task<bool> task = messageTarget(ea);
                task.Wait();
                if (!rabbitMQReceiver._autoAck)
                {
                    if (task.Result)
                    {
                        rabbitMQReceiver.Channel.BasicAck(ea.DeliveryTag, multiple: false);
                    }
                    else
                    {
                        rabbitMQReceiver.Channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
                    }
                }

                Thread.Sleep(settings.DelayBetweenMessages);
            }
            catch (AlreadyClosedException exception)
            {
                rabbitMQReceiver.Logger.LogWarning(exception, "Unable to process message because the connection has already been closed.");
            }
            catch (Exception ex)
            {
                Func<BasicDeliverEventArgs, Exception, bool> func = beforeHandleDequeueError;
                if (func == null || func(ea, ex))
                {
                    rabbitMQReceiver.HandleDequeueError(ea, ex);
                    afterHandleDequeueError?.Invoke(ea, ex);
                }
                else if (!rabbitMQReceiver._autoAck)
                {
                    rabbitMQReceiver.Channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                }
            }
        };
        _consumer.Received += _messageTargetDelegate;
    }

    public QuorumRabbitMQReceiver(
        QueueConfigurationSettings settings,
        Func<string, Task<bool>> messageTarget,
        ILogger logger,
        Func<string, Exception, bool> beforeHandleDequeueError = null,
        Action<string, Exception> afterHandleDequeueError = null)
    {
        QuorumRabbitMQReceiver rabbitMQReceiver = this;
        SetupReceiver(settings, logger);

        _afterHandleDequeueErrorFunc = afterHandleDequeueError;
        _beforeHandleDequeueErrorFunc = beforeHandleDequeueError;
        _delayBetweenMessages = settings.DelayBetweenMessages;
        _messageTargetFunc = messageTarget;
        _messageTargetDelegate = delegate (object model, BasicDeliverEventArgs ea)
        {
            string text = null;
            try
            {
                text = Encoding.UTF8.GetString(ea.Body.ToArray());
                Task<bool> task = messageTarget(text);
                task.Wait();
                if (!rabbitMQReceiver._autoAck)
                {
                    if (task.Result)
                    {
                        rabbitMQReceiver.Channel.BasicAck(ea.DeliveryTag, multiple: false);
                    }
                    else
                    {
                        rabbitMQReceiver.Channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
                    }
                }

                Thread.Sleep(settings.DelayBetweenMessages);
            }
            catch (AlreadyClosedException exception)
            {
                rabbitMQReceiver.Logger.LogWarning(exception, "Unable to process message because the connection has already been closed.");
            }
            catch (Exception ex)
            {
                Func<string, Exception, bool> func = beforeHandleDequeueError;
                if (func == null || func(text, ex))
                {
                    rabbitMQReceiver.HandleDequeueError(ea, ex);
                    afterHandleDequeueError?.Invoke(text, ex);
                }
                else if (!rabbitMQReceiver._autoAck)
                {
                    rabbitMQReceiver.Channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                }
            }
        };

        _consumer.Received += _messageTargetDelegate;
    }

    private void HandleDequeueError(BasicDeliverEventArgs ea, Exception rcvException)
    {
        Logger.LogError(rcvException, "Dequeue failed." + rcvException.Message);
        try
        {
            if (_useErrorQueue)
            {
                IBasicProperties basicProperties = _errorChannel.CreateBasicProperties();
                basicProperties.DeliveryMode = 2;
                try
                {
                    _errorChannel.BasicPublish("", ErrorQueueName, basicProperties, ea.Body);
                }
                catch (AlreadyClosedException)
                {
                    _errorChannel.WaitForAutoRecovery(_connection.Heartbeat);
                    _errorChannel.BasicPublish("", ErrorQueueName, basicProperties, ea.Body);
                }
            }

            if (!_autoAck)
            {
                Channel.BasicAck(ea.DeliveryTag, multiple: false);
            }
        }
        catch (Exception ex2)
        {
            Logger.LogError(ex2, "Publish to error queue failed." + ex2.Message);
            if (!_autoAck)
            {
                Channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
            }
        }
    }

    public void SetupReceiver(QueueConfigurationSettings settings, ILogger logger)
    {
        Logger = logger;
        QueueName = settings.QueueName;
        ErrorQueueName = settings.ErrorQueueName;
        Host = settings.Host;
        VirtualHost = settings.VirtualHost;
        _password = settings.Password;
        _userName = settings.UserName;
        _useErrorQueue = settings.UseErrorQueue;
        _autoAck = settings.AutoAcknowledge;
        Port = settings.Port;
        if (string.IsNullOrWhiteSpace(ErrorQueueName))
        {
            ErrorQueueName = QueueName + "Error";
        }

        _connection = RetryingConnectionFactory.GetConnection(_userName, _password, Port, VirtualHost, Host, settings.Heartbeat);
        var dictionary = new Dictionary<string, object>();
        dictionary.Add("x-queue-type", "quorum");
        if (settings is IConsumerTimeoutQueueConfiguration consumerTimeoutConfig)
        {
            if (consumerTimeoutConfig.ConsumerTimeout > 0)
            {
                dictionary.Add("x-consumer-timeout", consumerTimeoutConfig.ConsumerTimeout);
            }
        }
        if (settings.MaxPriority > 0 && dictionary["x-queue-type"].ToString() != "quorum")
        {
            dictionary.Add("x-max-priority", settings.MaxPriority);
        }

        Channel = _connection.CreateModel();
        Channel.QueueDeclare(QueueName, !settings.Exclusive ? true : false, settings.Exclusive, settings.Exclusive, dictionary);
        if (!string.IsNullOrEmpty(settings.Exchange))
        {
            Channel.QueueBind(QueueName, settings.Exchange, string.Empty);
        }

        if (_useErrorQueue)
        {
            dictionary.Clear();
            dictionary.Add("x-queue-type", "quorum");
            _errorChannel = _connection.CreateModel();
            _errorChannel.QueueDeclare(ErrorQueueName, durable: true, exclusive: false, autoDelete: false, dictionary);
        }

        Channel.BasicQos(0u, settings.PrefetchCount, global: false);
        _consumer = new EventingBasicConsumer(Channel);
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
    //     Dequeue the message
    public void Consume()
    {
        Channel.BasicConsume(QueueName, _autoAck, _consumer);
    }

    public void Stop()
    {
        if (!Channel.IsClosed)
        {
            Channel.Close();
        }

        if (_connection.IsOpen)
        {
            _connection.Close();
        }
    }

    //
    // Summary:
    //     Cleanup channel and connection
    public void Dispose()
    {
        if (!Channel.IsClosed)
        {
            Channel.Close();
        }

        if (_errorChannel != null && !_errorChannel.IsClosed)
        {
            _errorChannel.Close();
        }

        if (_connection.IsOpen)
        {
            _connection.Close();
        }

        Channel.Dispose();
        _connection.Dispose();
        Channel = null;
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
        if (Channel == null || Channel.IsClosed)
        {
            throw new AlreadyClosedException(new ShutdownEventArgs(ShutdownInitiator.Application, 200, "Goodbye"));
        }

        return Channel.QueueDelete(QueueName);
    }

    //
    // Summary:
    //     Deletes the error queue associated with this publisher.
    //
    // Returns:
    //     A System.UInt32 value indicating the number of messages purged during queue deletion.
    public uint DeleteErrorQueue()
    {
        if (Channel == null || Channel.IsClosed)
        {
            throw new AlreadyClosedException(new ShutdownEventArgs(ShutdownInitiator.Application, 200, "Goodbye"));
        }

        if (_useErrorQueue && !string.IsNullOrEmpty(ErrorQueueName))
        {
            return Channel.QueueDelete(ErrorQueueName);
        }

        return 0u;
    }

    public void UseMessageRepository<TMessage>(IMessageRepositoryFactory messageRepositoryFactory) where TMessage : class
    {
        var messageRepository = messageRepositoryFactory.CreateMessageRepository<TMessage>();

        if (messageRepository == null)
        {
            throw new ArgumentNullException(nameof(messageRepository), "Message repository cannot be null.");
        }

        QuorumRabbitMQReceiver rabbitMQReceiver = this;

        _consumer.Received -= _messageTargetDelegate;

        _messageTargetDelegate = delegate (object model, BasicDeliverEventArgs ea)
        {
            string text = null;
            IMessageData msgData = null;
            TMessage messageObj = null;
            string messageLockKey = null;
            try
            {
                text = Encoding.UTF8.GetString(ea.Body.ToArray());

                msgData = JsonSerializer.Deserialize<MessageData>(text);

                if (msgData.Uri != null)
                {
                    messageObj = messageRepository.Get(msgData);
                    messageLockKey = messageRepository.Lock(messageObj);
                    var jsonString = JsonSerializer.Serialize(messageObj);

                    Task<bool> task = _messageTargetFunc(jsonString);

                    task.Wait();

                    if (!rabbitMQReceiver._autoAck)
                    {
                        if (task.Result)
                        {
                            messageRepository.AckMessage(messageObj, messageLockKey);
                            rabbitMQReceiver.Channel.BasicAck(ea.DeliveryTag, multiple: false);
                        }
                        else
                        {
                            messageRepository.UnLock(messageObj, messageLockKey);
                            rabbitMQReceiver.Channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
                        }
                    }
                }
                else
                {
                    rabbitMQReceiver.Logger.LogWarning("Received message is not a valid MessageData object");
                    rabbitMQReceiver.Channel.BasicAck(ea.DeliveryTag, multiple: false);
                }

                Thread.Sleep(_delayBetweenMessages);
            }
            catch (AlreadyClosedException exception)
            {
                if (messageObj != null)
                {
                    messageRepository.UnLock(messageObj, messageLockKey);
                }

                rabbitMQReceiver.Logger.LogWarning(exception, "Unable to process message because the connection has already been closed.");
            }
            catch (Exception ex)
            {
                Func<string, Exception, bool> func = _beforeHandleDequeueErrorFunc;
                if (func == null || func(text, ex))
                {
                    if (msgData != null)
                    {
                        if (messageObj != null)
                        {
                            messageRepository.UnLock(messageObj, messageLockKey);
                        }
                        var errorResponse = new MessageDataErrorResponse(ex, includeStackTrace: true);
                        msgData.ErrorResponse = errorResponse;
                        byte[] msgBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(msgData, _jsonSerializerOptions));
                        ea.Body = msgBytes;
                        rabbitMQReceiver.HandleDequeueError(ea, ex);
                        _afterHandleDequeueErrorFunc?.Invoke(text, ex);
                    }
                    else
                    {
                        rabbitMQReceiver.Channel.BasicAck(ea.DeliveryTag, multiple: false);
                    }
                }
                else if (!rabbitMQReceiver._autoAck)
                {
                    rabbitMQReceiver.Channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                }
            }
        };

        _consumer.Received += _messageTargetDelegate;
    }
}
