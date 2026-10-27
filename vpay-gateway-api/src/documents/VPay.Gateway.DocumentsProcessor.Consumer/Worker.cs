namespace VPay.Gateway.DocumentsProcessor.Consumer;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IHostApplicationLifetime _hostLifetime;

    public Worker(ILogger<Worker> logger, IHostApplicationLifetime hostLifetime)
    {
        _logger = logger;
        _hostLifetime = hostLifetime;
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
            //_logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            await Task.Delay(5000, stoppingToken);
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

    private static string GetNamespace() => typeof(Startup).Namespace ?? throw new Exception("Malformed type");
}
