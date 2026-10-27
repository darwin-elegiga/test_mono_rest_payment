using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Models;
using VPay.Queuing;

namespace VPay.DocSys.Generation.QueueProcessor.Consolidated
{
    public class QueueProcessorService : IHostedService, IDisposable
    {
        private readonly ILogger _logger;
        private readonly MessageProcessor _messageProcessor;

        private readonly string _fullName;

        public QueueProcessorService(
            MessageProcessor messageProcessor,
            ILogger<QueueProcessorService> logger)
        {
            _fullName =  GetType().FullName;
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

#if DEBUG
            Console.WriteLine("Any key to continue.");
            Console.ReadLine();
#endif

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
