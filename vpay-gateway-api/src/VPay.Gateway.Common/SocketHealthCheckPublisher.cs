using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace VPay.Gateway.Common;

public class SocketHealthCheckPublisher : IHealthCheckPublisher
{
    private readonly TcpListener _listener;

    public SocketHealthCheckPublisher(int port)
    {
        _listener = new TcpListener(IPAddress.Any, port);
    }

    public Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
    {
        if (report.Status == HealthStatus.Healthy)
        {
            _listener.Start();
        }
        else
        {
            _listener.Stop();
        }
        return Task.CompletedTask;
    }
}

public static class HealthCheckBuilderExtensions
{
    public static IServiceCollection AddSocketListener(this IServiceCollection services, int port)
    {
        services.Configure<HealthCheckPublisherOptions>(options =>
        {
            options.Delay = TimeSpan.FromSeconds(10);
            options.Period = TimeSpan.FromSeconds(120);
            options.Timeout = TimeSpan.FromSeconds(60);
        });
        services
            //.AddTransient<HealthCheckPublisherOptions, HealthCheckPublisherOptions>()
            .AddSingleton<IHealthCheckPublisher>(opt => ActivatorUtilities.CreateInstance<SocketHealthCheckPublisher>(opt, port));
        return services;
    }
}
