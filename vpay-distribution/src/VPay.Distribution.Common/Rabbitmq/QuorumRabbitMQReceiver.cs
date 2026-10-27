using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using VPay.Queuing;

namespace VPay.Distribution.Common.Rabbitmq
{
    public class QuorumRabbitMQReceiver : IMessageReceiver, IDisposable
    {
        private IConnection _connection;

        public IModel Channel;

        private string _password;

        private string _userName;

        private EventingBasicConsumer _consumer;

        private IModel _errorChannel;

        private bool _useErrorQueue;

        private bool _autoAck;

        public string QueueName { get; set; }

        public string ErrorQueueName { get; set; }

        public string Host { get; set; }

        public string VirtualHost { get; set; }

        public int Port { get; set; }

        public ILogger Logger { get; set; }

        public QuorumRabbitMQReceiver(QueueConfigurationSettings settings, Func<BasicDeliverEventArgs, Task<bool>> messageTarget, ILogger logger, Func<BasicDeliverEventArgs, Exception, bool> beforeHandleDequeueError = null, Action<BasicDeliverEventArgs, Exception> afterHandleDequeueError = null)
        {
            QuorumRabbitMQReceiver rabbitMQReceiver = this;
            SetupReceiver(settings, logger);
            _consumer.Received += delegate (object model, BasicDeliverEventArgs ea)
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
        }

        public QuorumRabbitMQReceiver(QueueConfigurationSettings settings, Func<string, Task<bool>> messageTarget, ILogger logger, Func<string, Exception, bool> beforeHandleDequeueError = null, Action<string, Exception> afterHandleDequeueError = null)
        {
            QuorumRabbitMQReceiver rabbitMQReceiver = this;
            SetupReceiver(settings, logger);
            _consumer.Received += delegate (object model, BasicDeliverEventArgs ea)
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
            if (settings.MaxPriority > 0)
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
    }
}
