using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Newtonsoft.Json;

using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Common.Helpers;
using VPay.DocSys.Generation.Models;
using VPay.Queuing;
using static VPay.DocSys.Common.ArgumentHelpers;
using VPay.DocSys.Generation.Common.Rabbitmq;

namespace VPay.DocSys.Generation.FaxRetry.QueueProcessor
{
    public class MessageProcessor
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly QueueConfigurations _queueConfigurations;
        private readonly ProcessorSettings _processorSettings;
        private readonly QueueConfigurationSettings _queueConfigurationSettings;
        private readonly IFaxArchiveMessageProcessor _faxArchiveMessageProcessor;
        private readonly ILogger _logger;

        private  IMessageReceiver _queueReceiver;

        private int _processCounter;

        public MessageProcessor(
            IServiceProvider serviceProvider,
            ILogger<MessageProcessor> logger)
        {
            ThrowIf.IsNull(logger, nameof(logger));
            _serviceProvider = serviceProvider;
            using (var scope = serviceProvider.CreateScope())
            {
                _processorSettings = scope.ServiceProvider
                         .GetRequiredService<ProcessorSettings>();
                _queueConfigurations = scope.ServiceProvider
                            .GetRequiredService<QueueConfigurations>();
                _faxArchiveMessageProcessor = scope.ServiceProvider
                        .GetRequiredService<IFaxArchiveMessageProcessor>();
            }
            
            _queueConfigurationSettings = _queueConfigurations.FaxRetryQueue;
            
            _logger = logger;

            //_queueReceiver = QuorumReceiverFactory.GetReceiver(_queueConfigurationSettings, async s => await ProcessMessage(s), _logger);
        }

        public void Start()
        {
            using (_logger.BeginScope(new Dictionary<string, object>()
            {
                { "QueueName", _queueConfigurationSettings.QueueName },
                { "ErrorQueueName", _queueConfigurationSettings.ErrorQueueName },
                { "Exchange", _queueConfigurationSettings.Exchange },
                { "Host", _queueConfigurationSettings.Host },
                { "VirtualHost", _queueConfigurationSettings.VirtualHost },
                { "UserName", _queueConfigurationSettings.UserName },
                { "Port", _queueConfigurationSettings.Port },
                { "MaxPriority", _queueConfigurationSettings.MaxPriority },
            }))
            {
                _logger.LogInformation($"{this.GetType().FullName} starting.");
            }

            _queueReceiver = QuorumReceiverFactory.GetReceiver(_queueConfigurationSettings, async (string s) => await this.ProcessMessage(s), _logger);

            //_logger.LogInformation($"{this.GetType().FullName} starting...");

            _queueReceiver.Consume();
        }

        public void Stop()
        {
            _logger.LogInformation($"{GetType().FullName} stopping...");

            _queueReceiver.Stop();
        }

        public async Task<bool> ProcessMessage(string message)
        {
            _logger.LogInformation($"Started Executing method {GetType().FullName}.ProcessMessage");

            if (++_processCounter > _processorSettings.GarbageCollectionInterval)
            {
                GC.Collect();
                _processCounter = 0;

                string infoMsg = $"Calling GC after {_processCounter} loops.";

                _logger.LogInformation(infoMsg);
            }

            var payload = JsonConvert.DeserializeObject<FaxRetryArchiveMessage>(message);

            if (payload == null)
            {
                _logger.LogError("Could not deserialize message: {Message}", message);
            }

            using (new PerfTimerLogger(_logger, "Process Fax Retry Archive message"))
            using (var scope = _serviceProvider.CreateScope())
            {
                using (GetPayloadLogScope(payload))
                {
                    try
                    {
                        var scopedProcessingService =
                            scope.ServiceProvider
                                .GetRequiredService<IFaxArchiveMessageProcessor>();

                        await scopedProcessingService.ProcessMessage(payload);
                    }
                    catch (Exception exception)
                    {
                        _logger.LogError(exception, "An error occurred handling Fax Retry Archive: {Message}", exception.Message);

                        throw;
                    }
                }
            }

            return true;
        }

        private IDisposable GetPayloadLogScope(FaxRetryArchiveMessage faxRetryArchiveMessage)
        {
            var logScopeDictionary = faxRetryArchiveMessage.ToDictionaryExcept(nameof(FaxRetryArchiveMessage.TransactionIds));
            logScopeDictionary.Add("TransactionIds", string.Join(", ", faxRetryArchiveMessage.TransactionIds));

            return _logger.BeginScope(logScopeDictionary);
        }
    }
}
