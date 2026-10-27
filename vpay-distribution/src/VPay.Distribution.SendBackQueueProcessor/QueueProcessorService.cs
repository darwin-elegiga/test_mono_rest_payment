using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using VPay.Queuing;

namespace VPay.Distribution.SendBackQueueProcessor
{
    public class QueueProcessorService : IHostedService
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;

        private readonly MessageProcessor _messageProcessor;

        public QueueProcessorService(
            ILogger<QueueProcessorService> logger,
            IConfiguration configuration,
            MessageProcessor messageProcessor)
        {
            _logger = logger;
            _configuration = configuration;
            _messageProcessor = messageProcessor;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{typeof(QueueProcessorService).FullName} Starting.");

            var queueSettings = _configuration.GetSection("SendBackDistributionQueue").Get<QueueConfigurationSettings>();

            try
            {
                _messageProcessor.Start(queueSettings, _logger);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in message processor");
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _messageProcessor.Stop();
            _logger.LogInformation("Shutdown occurred for VPay.Distribution.SendBackQueueProcessor");
            return Task.CompletedTask;
        }
    }
}
