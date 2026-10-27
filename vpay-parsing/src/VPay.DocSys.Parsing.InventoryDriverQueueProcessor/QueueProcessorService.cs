using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VPay.DocSys.Parsing.Common;
using VPay.DocSys.Parsing.Common.Interfaces;
using VPay.Queuing;

namespace VPay.DocSys.Parsing.InventoryDriverQueueProcessor
{
    public class QueueProcessorService : IHostedService, IDisposable
    {
        private readonly ILogger _logger;
        private readonly string _fullName;
        private readonly MessageProcessor _messageProcessor;

        public QueueProcessorService(
            MessageProcessor messageProcessor,
            ILogger<QueueProcessorService> logger)
        {
            _fullName = GetType().FullName;
            _logger = logger;
            _messageProcessor = messageProcessor;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Initial Startup occurred for {_fullName}");

            try
            {
                _messageProcessor.Start();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unhandled exception in {_fullName}");
            }
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _messageProcessor.Stop();
            _logger.LogInformation($"Shutdown occurred for {_fullName}");
            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }
    }
}
