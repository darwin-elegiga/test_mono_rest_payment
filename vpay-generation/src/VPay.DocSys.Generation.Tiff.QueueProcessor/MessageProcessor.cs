using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Common.Helpers;
using VPay.DocSys.Generation.Models;
using VPay.Queuing;
using VPay.DocSys.Generation.Common.Rabbitmq;

namespace VPay.DocSys.Generation.Tiff.QueueProcessor
{
    public class MessageProcessor
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly QueueConfigurations _queueConfigurations;
        private readonly ProcessorSettings _processorSettings;
        private readonly QueueConfigurationSettings _queueConfigurationSettings;
        private readonly IMessageReceiver _queueReceiver;
        private readonly ILogger _logger;

        private int _processCounter;

        public MessageProcessor(
            IServiceProvider serviceProvider,
            ILogger<MessageProcessor> logger)
        {
            _serviceProvider = serviceProvider;
            
            using (var scope = serviceProvider.CreateScope())
            {
                _processorSettings = scope.ServiceProvider
                    .GetRequiredService<ProcessorSettings>();
                _queueConfigurations = scope.ServiceProvider
                         .GetRequiredService<QueueConfigurations>();
            }
            
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _queueConfigurationSettings = _queueConfigurations.TiffQueue ?? throw new ArgumentNullException(nameof(_queueConfigurations.TiffQueue));

            _queueReceiver = QuorumReceiverFactory.GetReceiver(_queueConfigurationSettings, async s => await ProcessMessage(s), _logger);
        }

        public void Start()
        {
            _logger.LogInformation($"{this.GetType().FullName} starting.");

            _queueReceiver.Consume();
        }

        public void Stop()
        {
            _queueReceiver.Stop();
        }

        public async Task<bool> ProcessMessage(string message)
        {
            if(++_processCounter > _processorSettings.GarbageCollectionInterval)
            {
                GC.Collect();
                _processCounter = 0;
            }

            using(new PerfTimerLogger(_logger, "Process Pdf to Tiff Conversion message"))
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    try
                    {
                        var scopedProcessingService =
                              scope.ServiceProvider
                                .GetRequiredService<ITiffQueueMessageProcessor>();

                        await scopedProcessingService.ProcessMessageString(message);
                    }
                    catch (Exception exception)
                    {
                        _logger.LogError(exception, "An error occurred handling a Pdf to Tiff Conversion: {Message}", exception.Message);
                        throw;
                    }
                }
            }

            return true;
        }
    }
}
