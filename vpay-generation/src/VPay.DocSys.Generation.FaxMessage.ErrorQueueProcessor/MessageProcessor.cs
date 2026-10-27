using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Models;
using VPay.Queuing;
using VPay.DocSys.Generation.Common.Rabbitmq;

namespace VPay.DocSys.Generation.FaxMessage.ErrorQueueProcessor
{
    public class MessageProcessor
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly QueueConfigurationSettings _errorQueue;
        private readonly ProcessorSettings _processorSettings;
        private readonly QueueConfigurations _queueConfigurations;
        private readonly IQueueService _queueService;
        private readonly IMessageReceiver _queueReceiver;
        private readonly ILogger _logger;

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
                _queueService = scope.ServiceProvider
                             .GetRequiredService<IQueueService>();
                _queueConfigurations = scope.ServiceProvider
                             .GetRequiredService<QueueConfigurations>();
            }
           
            _logger = logger;

            try
            {
                _errorQueue = _queueService.GetErrorQueueConfiguration(_queueConfigurations.FaxMessageQueue);
                _queueReceiver = QuorumReceiverFactory.GetReceiver(_errorQueue, async s => await ProcessMessage(s), _logger);
            }
            catch (Exception ex)
            {
                throw ex;
            }
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

        public Task<bool> ProcessMessage(string message)
        {
            if(++_processCounter > _processorSettings.GarbageCollectionInterval)
            {
                GC.Collect();
                _processCounter = 0;
            }

            using(new PerfTimerLogger(_logger, "Process error queue fax message"))
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    try
                    {
                        var scopedProcessingService =
                                 scope.ServiceProvider
                                    .GetRequiredService<IFaxMessageErrorQueueMessageProcessor>();

                        scopedProcessingService.ProcessMessageString(message, _processorSettings.MaxRetryCount);
                    }
                    catch (Exception exception)
                    {
                        _logger.LogError(exception, "An error occurred handling an Error PMB message {Message}", exception.Message);
                        throw;
                    }
                }
            }

            Thread.Sleep(_processorSettings.SleepTimeInSeconds * 1000);
            return Task.FromResult(true);
        }
    }
}
