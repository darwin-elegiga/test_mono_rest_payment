using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace VPay.Optum.PaymentRouting.InboundPayment.Consumers
{
    [ExcludeFromCodeCoverage]
    public class LifetimeEventsHostedService : IHostedService
    {
        private readonly ILogger<LifetimeEventsHostedService> _logger;
        private readonly IHostApplicationLifetime _hostLifetime;

        public LifetimeEventsHostedService(ILogger<LifetimeEventsHostedService> logger, IHostApplicationLifetime hostLifetime)
        {
            _logger = logger;
            _hostLifetime = hostLifetime;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _hostLifetime.ApplicationStarted.Register(() => OnStarted(_logger));
            _hostLifetime.ApplicationStopping.Register(() => OnStopping(_logger));
            _hostLifetime.ApplicationStopped.Register(() => OnStopped(_logger));

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private void OnStarted(ILogger logger)
        {
            logger.LogInformation($"Startup occurred for {GetNamespace()}");
        }

        private void OnStopping(ILogger logger)
        {
            logger.LogInformation($"Shutdown occurred for {GetNamespace()}");
        }

        private void OnStopped(ILogger logger)
        {
            logger.LogInformation($"Shutdown completed for {GetNamespace()}");
        }

        private static string GetNamespace() => typeof(Startup).Namespace ?? throw new Exception("Malformed type");
    }
}
