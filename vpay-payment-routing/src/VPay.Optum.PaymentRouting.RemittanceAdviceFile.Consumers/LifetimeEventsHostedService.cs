using System.Diagnostics.CodeAnalysis;

namespace VPay.Optum.PaymentRouting.RemittanceAdviceFile.Consumers
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

        private static void OnStarted(ILogger logger)
        {
            logger.LogInformation($"Startup occurred for VPay.Optum.PaymentRouting.RemittanceAdviceFile.Consumers");
        }

        private static void OnStopping(ILogger logger)
        {
            logger.LogInformation($"Shutdown occurred for VPay.Optum.PaymentRouting.RemittanceAdviceFile.Consumers");
        }

        private static void OnStopped(ILogger logger)
        {
            logger.LogInformation($"Shutdown completed for VPay.Optum.PaymentRouting.RemittanceAdviceFile.Consumers");
        }

    }
}
