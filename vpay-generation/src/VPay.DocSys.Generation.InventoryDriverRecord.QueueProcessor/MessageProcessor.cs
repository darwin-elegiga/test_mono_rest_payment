using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Models;
using VPay.Queuing;
using VPay.DocSys.Generation.Common.Rabbitmq;

namespace VPay.DocSys.Generation.InventoryDriverRecord.QueueProcessor
{
    public class MessageProcessor
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly QueueConfigurations _queueConfigurations;
        private readonly ProcessorSettings _processorSettings;
        private readonly QueueConfigurationSettings _queueConfigurationSettings;
        private readonly ILogger _logger;

        private IMessageReceiver _queueReceiver;
        private int _processCounter;

        public MessageProcessor(
            IServiceProvider serviceProvider,
            ILogger<MessageProcessor> logger)
        {
            ArgumentHelpers.ThrowIf.IsNull(logger, nameof(logger));
            _serviceProvider = serviceProvider;
            using (var scope = serviceProvider.CreateScope())
            {
                _processorSettings = scope.ServiceProvider
                        .GetRequiredService<ProcessorSettings>();
                _queueConfigurations = scope.ServiceProvider
                            .GetRequiredService<QueueConfigurations>();
            }

            _queueConfigurationSettings = _queueConfigurations.InventoryDriverRecordQueue;

            _logger = logger;
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

            var messageType = typeof(DocumentTransactionMessage);

            _queueReceiver = QuorumReceiverFactory.GetReceiver(
                _queueConfigurationSettings,
                async (string s) => await this.ProcessMessage(s),
                _logger,
                beforeHandleDequeueError: null,
                afterHandleDequeueError: null);
            _queueReceiver.Consume();
        }

        public void Stop()
        {
            _queueReceiver.Stop();
        }

        public async Task<bool> ProcessMessage(string message)
        {
            if (++_processCounter > _processorSettings.GarbageCollectionInterval)
            {
                GC.Collect();
                _processCounter = 0;
            }

            var payload = JsonConvert.DeserializeObject<InventoryDriverRecordMessage>(message);

            using (BeginLogScope(payload))
            using (new PerfTimerLogger(_logger, "Executed method InventoryDriverRecordMessageProcessor.Process"))
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    try
                    {
                        var scopedProcessingService =
                               scope.ServiceProvider
                                   .GetRequiredService<IInventoryDriverRecordMessageProcessor>();

                        await scopedProcessingService.Process(payload);

                        _logger.LogInformation("Started Executing method InventoryDriverRecordMessageProcessor.Process");
                    }
                    catch (Exception exception)
                    {
                        _logger.LogError(exception, $"An error occurred handling an inventory driver record : {exception.Message}");
                        throw;
                    }
                }
            }

            return true;
        }

        private IDisposable BeginLogScope(InventoryDriverRecordMessage payload)
        {
            return _logger.BeginScope(new Dictionary<string, object>
            {
                {"TPA", payload.Tpa},
                {"InventoryDriverFilePath", payload.InventoryDriverFilePath},
                {"DocumentType", payload.DocumentType},
                {"TransactionId", payload.TransactionId},
                {"Guid", payload.Guid},
                {"PaymentId", payload.PaymentId},
            });
        }
    }
}
