using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using VPay.Queuing;

namespace VPay.Distribution.RedCardQueueProcessor
{
    public class QueueProcessorService : IHostedService, IDisposable
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private MessageProcessor _messageProcessor;

        public QueueProcessorService(
            IConfiguration configuration,
            ILogger<QueueProcessorService> logger,
            MessageProcessor messageProcessor)
        {
            _logger = logger;
            _messageProcessor = messageProcessor;
            _configuration = configuration;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{typeof(QueueProcessorService).FullName} Starting.");

            var queueSettings = _configuration.GetSection("RedCardDistributionQueue").Get<QueueConfigurationSettings>();

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
            _logger.LogInformation("Shutdown occurred for VPay.Distribution.RedCardQueueProcessor");
            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }
    }
}
