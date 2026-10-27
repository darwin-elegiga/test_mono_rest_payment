using System;
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
using VPay.DocSys.Generation.Common.Rabbitmq;
using System.Collections.Generic;

namespace VPay.DocSys.Generation.RFax.QueueProcessor
{
    public class MessageProcessor
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly QueueConfigurations _queueConfigurations;
        private readonly ProcessorSettings _processorSettings;
        private readonly QueueConfigurationSettings _queueConfigurationSettings;
        private  IMessageReceiver _queueReceiver;
        private readonly ILogger _logger;

        private int _processCounter;

        public MessageProcessor(
            IServiceProvider serviceProvider,
            ILogger<MessageProcessor> logger)
        {
            _serviceProvider = serviceProvider;
            
            using(var scope = serviceProvider.CreateScope())
            {
                _processorSettings = scope.ServiceProvider
                         .GetRequiredService<ProcessorSettings>();
                _queueConfigurations = scope.ServiceProvider
                        .GetRequiredService<QueueConfigurations>();
            }
            
            _queueConfigurationSettings = _queueConfigurations.ReminderFaxQueue ?? throw new ArgumentNullException(nameof(_queueConfigurations.ReminderFaxQueue));
            _queueReceiver = QuorumReceiverFactory.GetReceiver(_queueConfigurationSettings, async s => await ProcessMessage(s), _logger);
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
            //_logger.LogInformation($"{this.GetType().FullName} starting.");

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

            try
            {
                var deserialized = JsonConvert.DeserializeObject<MessageWrapper>(message);
                var messageType = Type.GetType(deserialized.MessageType);
                var obj = JsonConvert.DeserializeObject(Convert.ToString(deserialized.Message), messageType);
                using (var scope = _serviceProvider.CreateScope())
                {
                    if (messageType == typeof(ReminderFaxMessage))
                    {
                        using (new PerfTimerLogger(_logger, "Process Reminder Fax"))
                        {

                            var scopedProcessingService =
                                 scope.ServiceProvider
                                    .GetRequiredService<IReminderFaxMessageProcessor>();

                            await scopedProcessingService.ProcessMessage(obj as ReminderFaxMessage);
                        }
                    }
                    else if (messageType == typeof(ReminderFaxQueueMessage))
                    {
                        using (new PerfTimerLogger(_logger, "Process Reminder Faxes"))
                        {
                            var scopedProcessingService =
                                  scope.ServiceProvider
                                    .GetRequiredService<IReminderFaxService>();

                            await scopedProcessingService.ProcessReminderFaxes(obj as ReminderFaxQueueMessage);
                        }
                    }
                    else
                    {
                        throw new Exception($"Invalid Message Type {deserialized.MessageType}");
                    }
                }
            }
            catch (Exception e) when (e.LogWithScope(_logger, message: "An error occurred handling a Reminder Fax Payload"))
            {
                throw;
            }

            return true;
        }
    }
}
