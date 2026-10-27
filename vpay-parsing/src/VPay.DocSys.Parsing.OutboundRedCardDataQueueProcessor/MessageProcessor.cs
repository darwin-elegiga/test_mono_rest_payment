using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using VPay.DocSys.Common;
using VPay.DocSys.Parsing.Common;
using VPay.DocSys.Parsing.Common.Interfaces;
using VPay.Queuing;
using VPay.DocSys.Parsing.Common.Rabbitmq;

namespace VPay.DocSys.Parsing.OutboundRedCardDataQueueProcessor
{
    public class MessageProcessor
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;
        

        private readonly int _requeueDelayInSeconds = 120;
        private readonly int _processCounterDoGc = 10;

        private int _processCounter;

        private IMessageReceiver _queueReceiver;
        private IMessagePublisher _queuePublisher;

        public MessageProcessor(
           IServiceProvider serviceProvider,
            IConfiguration config,
            ILogger<MessageProcessor> logger)
        {
            _processCounterDoGc = config.GetSection("QueueProcessor").GetValue("DoGCAfter", _processCounterDoGc );
            _requeueDelayInSeconds = config.GetSection("QueueProcessor").GetValue("RequeueDelayInSeconds", _requeueDelayInSeconds);
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public void Start()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var _queueSettings =
                    scope.ServiceProvider
                        .GetRequiredService<OutboundRedCardDataQueueSettings>();

                using (_logger.BeginScope(new Dictionary<string, object>()
                {
                    { "QueueName", _queueSettings.QueueName },
                    { "ErrorQueueName", _queueSettings.ErrorQueueName },
                    { "Exchange", _queueSettings.Exchange },
                    { "Host", _queueSettings.Host },
                    { "VirtualHost", _queueSettings.VirtualHost },
                    { "UserName", _queueSettings.UserName },
                    { "Port", _queueSettings.Port },
                    { "MaxPriority", _queueSettings.MaxPriority },
                }))
                {
                    _logger.LogInformation($"{this.GetType().FullName} starting.");
                }

                _queueReceiver = QuorumReceiverFactory.GetReceiver(_queueSettings, this.ProcessMessage, _logger);

                _queuePublisher = QuorumPublisherFactory.GetPublisher(_queueSettings, _logger);

                _queueReceiver.Consume();
            }
        }

        public void Stop()
        {
            _queueReceiver.Stop();
        }

        public async Task<bool> ProcessMessage(string message)
        {
            if(++_processCounter > _processCounterDoGc)
            {
                GC.Collect();
                _processCounter = 0;
            }

            var msg = JsonConvert.DeserializeObject<RedCardParsingMessage>(message);

            return await this.ProcessMessage(msg);
        }

        private async Task<bool> ProcessMessage(RedCardParsingMessage message)
        {
            if(message.NextStatusCheck > DateTime.Now)
            {
                return await RequeueMessage(message);
            }

            using(BeginLogScope(message))
            {
                _logger.LogInformation($"Processing Message");

                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var scopedProcessingService =
                            scope.ServiceProvider
                                .GetRequiredService<IOutboundRedCardDataDriverFileService>();

                        var result = await scopedProcessingService.ParseFile(message.FilePath);
                  
                        if (result.ResultCode == RedCardProcessResultCode.Error)
                        {
                            throw new Exception(result.Description);
                        }
                        else if (result.ResultCode == RedCardProcessResultCode.Retry)
                        {
                            await this.RequeueMessageWithDelay(message, result.Description);
                        }
                    }
                }
                catch(Exception exception)
                {
                    while(exception is AggregateException && exception.InnerException != null)
                    {
                        exception = exception.InnerException;
                    }

                    _logger.LogError(exception, $"An error occurred with parsing : {exception.Message}");

                    throw;
                }
            }

            return true;
        }

        private async Task<bool> RequeueMessage(RedCardParsingMessage message)
        {
            var msgText = JsonConvert.SerializeObject(message);

            _queuePublisher.Publish(msgText);

            return await Task.FromResult(true);
        }

        private async Task<bool> RequeueMessageWithDelay(RedCardParsingMessage message, string reasonDesc)
        {
            message.DelayCount++;

            message.NextStatusCheck = DateTime.Now.Add(TimeSpan.FromSeconds(_requeueDelayInSeconds));

            var msgText = JsonConvert.SerializeObject(message);

            _queuePublisher.Publish(msgText);

            _logger.LogWarning($"Message Requeued (x{message.DelayCount}): {reasonDesc}");

            return await Task.FromResult(true);
        }

        private IDisposable BeginLogScope(RedCardParsingMessage message)
        {
            return _logger.BeginScope(new Dictionary<string, object>
            {
                { "MessageType", nameof(RedCardParsingMessage) },
                { "CreateDate", message.MessageCreated },
                { "FilePath",  message.FilePath }
            });
        }
    }
}
