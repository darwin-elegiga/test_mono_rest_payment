using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VPay.DocSys.Parsing.Common;
using VPay.DocSys.Parsing.Common.Interfaces;
using VPay.Queuing;
using VPay.DocSys.Parsing.Common.Rabbitmq;

namespace VPay.DocSys.Parsing.QueueProcessor.Consolidated
{
    public class MessageProcessor
    {
        private readonly IServiceProvider _serviceProvider;
        private int _processCounter;
        private readonly int _processCounterDoGc =10;

        private ILogger _logger;
        private IMessageReceiver _queueReceiver;

        public MessageProcessor(
            IServiceProvider serviceProvider,
            IConfiguration config,
            ILogger<MessageProcessor> logger)
        {
            _processCounterDoGc = config.GetSection("QueueConsumer").GetValue<int>("DoGCAfter", _processCounterDoGc);
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public void Start()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var queueSettings =
                    scope.ServiceProvider
                        .GetRequiredService<StatusFileParsingQueueSettings>();

                using (_logger.BeginScope(new Dictionary<string, object>()
                {
                    { "QueueName", queueSettings.QueueName },
                    { "ErrorQueueName", queueSettings.ErrorQueueName },
                    { "Exchange", queueSettings.Exchange },
                    { "Host", queueSettings.Host },
                    { "VirtualHost", queueSettings.VirtualHost },
                    { "UserName", queueSettings.UserName },
                    { "Port", queueSettings.Port },
                    { "MaxPriority", queueSettings.MaxPriority },
                }))
                {
                    _logger.LogInformation($"{this.GetType().FullName} starting.");
                }
          
                _queueReceiver = QuorumReceiverFactory.GetReceiver(queueSettings, this.ProcessMessage, _logger);

                _queueReceiver.Consume();
            }
        }

        public void Stop()
        {
            _queueReceiver.Stop();
        }

        public Task<bool> ProcessMessage(string message)
        {
            if (++_processCounter > _processCounterDoGc)
            {
                GC.Collect();
                _processCounter = 0;
            }
            var payload = PreparePayload(message);
            string additionalInfo = $"{{StatusFilePath}}={payload.StatusFilePath ?? "NULL"}";
            using (BeginLogScope(payload))
            using (new PerfTimerLogger(_logger, $"Executed method VPay.DocSys.Parsing.QueueProcessor.Consolidated.ProcessMessage - {additionalInfo}"))
            {
                try
                {
                        using (var scope = _serviceProvider.CreateScope())
                        {
                            var scopedProcessingService =
                                scope.ServiceProvider
                                    .GetRequiredService<IStatusFileService>();

                            scopedProcessingService.ParseAndQueueTransactions(
                                payload.StatusFilePath,
                                payload.TransactionIds,
                                payload.IsRegenRequest).Wait();
                        }

                }
                catch (Exception exception)
                {
                    while(exception is AggregateException && exception.InnerException != null)
                    {
                        exception = exception.InnerException;
                    }

                    _logger.LogError(exception, $"An error occurred with parsing : {exception.Message}");

                    throw;
                }
            }
            return Task.FromResult(true);
        }

        private IDisposable BeginLogScope(ParsingMessage payload)
        {
            return _logger.BeginScope(new Dictionary<string, object>
            {
                { "StatusFilePath",  payload.StatusFilePath }
            });
        }

        public ParsingMessage PreparePayload(string message)
        {
            var msgContent = JsonConvert.DeserializeObject<ParsingMessage>(message);
            return msgContent;
        }
    }
}
