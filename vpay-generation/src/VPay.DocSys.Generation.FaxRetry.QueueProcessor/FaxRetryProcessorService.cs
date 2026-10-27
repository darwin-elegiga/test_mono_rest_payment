using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace VPay.DocSys.Generation.FaxRetry.QueueProcessor
{
    public class FaxRetryProcessorService : IHostedService, IDisposable
    {
        private readonly ILogger _logger;
        private readonly MessageProcessor _messageProcessor;

        private readonly string _fullName;

        public FaxRetryProcessorService(
            MessageProcessor messageProcessor,
            ILogger<FaxRetryProcessorService> logger)
        {
            _fullName = GetType().FullName;
            _messageProcessor = messageProcessor;
            _logger = logger;
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

            if (Debugger.IsAttached)
            {
                Console.WriteLine("Any key to continue.");
                Console.ReadLine();
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
