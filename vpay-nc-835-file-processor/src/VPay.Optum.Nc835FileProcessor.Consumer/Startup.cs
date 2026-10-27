using System.IO.Abstractions;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using VPay.Extensions.HealthChecks;
using VPay.MassTransit.Abstractions;
using VPay.MassTransit.DependencyInjection;
using VPay.Optum.Data.Sql.DependencyInjection;
using VPay.Optum.Data.Sql.Health;
using VPay.Optum.Nc835FileProcessor.Application.HealthChecks;
using VPay.Optum.Nc835FileProcessor.Application.Models;
using VPay.Optum.Nc835FileProcessor.Application.Services;
using VPay.Optum.Nc835FileProcessor.Consumer.Services;

namespace VPay.Optum.Nc835FileProcessor.Consumer
{
    public static class Startup
    {
        public static void ConfigureServices(HostBuilderContext hostContext, IServiceCollection services)
        {
            //Recursively get assemblies that are referenced by the API assembly
            var vPayAssemblies = AssemblyScanning.GetVPayAssemblies(typeof(Startup));

            var configuration = hostContext.Configuration;

            services.AddOptions();

            services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(vPayAssemblies));

            services
                .Configure<Nc835FileSettings>(configuration.GetSection("Settings"))
                .AddTransient(cfg => cfg.GetService<IOptions<Nc835FileSettings>>().Value)

                .Configure<TransportConfig>(configuration.GetSection("RabbitQueue"))
                .AddTransient(cfg => cfg.GetService<IOptions<TransportConfig>>().Value)

                .AddTransient<IFileSystem, FileSystem>()
                .AddTransient<IX12Parser, X12Parser>();

            services
                .AddSql(config => configuration.Bind("OptumIntegrationDatabase", config))
                .WithTypeMappings(vPayAssemblies);

            services
                .AddWorkerHealthCheckPublisher()
                .AddHealthChecks()
                .AddCheck<BaseFolderPathHealthCheck>("BaseFolderPath")
                .AddCheck<SqlServerSystemHealthCheck>("sql-server");

            var rabbitConfig = new TransportConfig();
            configuration.Bind("RabbitQueue", rabbitConfig);

            services.UseMassTransit(rabbitConfig, opts =>
            {
                opts.AddConsumer<Nc835FileNotificationConsumer, Nc835FileNotificationConsumerDefinition>();
            });

            services.UseMassTransitActivityTracking();
            services.AddMassTransitHostedService();

            services.AddHostedService<LifetimeEventsHostedService>();
        }
    }
}
