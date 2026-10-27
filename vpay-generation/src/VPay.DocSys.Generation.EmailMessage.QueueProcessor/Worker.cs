using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace VPay.DocSys.Generation.EmailMessage.QueueProcessor;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IHostApplicationLifetime _hostLifetime;

    public Worker(ILogger<Worker> logger, IHostApplicationLifetime hostApplicationLifetime)
    {
        _logger = logger;
        _hostLifetime = hostApplicationLifetime;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _hostLifetime.ApplicationStarted.Register(() => OnStarted(_logger));
        _hostLifetime.ApplicationStopping.Register(() => OnStopping(_logger));
        _hostLifetime.ApplicationStopped.Register(() => OnStopped(_logger));
        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(10000, stoppingToken);
        }
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

    private string GetNamespace() => GetType().Namespace ?? throw new Exception("Malformed type");
}
