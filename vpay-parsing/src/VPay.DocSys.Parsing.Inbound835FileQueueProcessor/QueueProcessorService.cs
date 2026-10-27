using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace VPay.DocSys.Parsing.Inbound835FileQueueProcessor
{
    public class QueueProcessorService : IHostedService, IDisposable
    {
        private readonly ILogger _logger;
        private readonly MessageProcessor _messageProcessor;

        public QueueProcessorService(
            MessageProcessor messageProcessor,
            ILogger<QueueProcessorService> logger)
        {
            _logger = logger;
            _messageProcessor = messageProcessor;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{typeof(QueueProcessorService).FullName} Starting.");
            
            try
            {
                _messageProcessor.Start();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Unhandled exception in message processor");
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _messageProcessor.Stop();
            _logger.LogInformation("Shutdown occurred for VPay.DocSys.Parsing.Inbound835FileQueueProcessor");
            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }
    }
}
