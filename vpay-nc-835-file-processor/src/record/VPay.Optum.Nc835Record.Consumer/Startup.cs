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
using VPay.Optum.Http;
using VPay.Optum.Http.ConfigApi;
using VPay.Optum.Http.DocSysGeneration;
using VPay.Optum.Http.Loaf;
using VPay.Optum.Http.Transactions;
using VPay.Optum.Nc835Record.Application.Models;
using VPay.Optum.Nc835Record.Consumer.Services;

namespace VPay.Optum.Nc835Record.Consumer
{
    public static class Startup
    {
        public static void ConfigureServices(HostBuilderContext hostContext, IServiceCollection services)
        {
            //Recursively get assemblies that are referenced by the API assembly
            var vPayAssemblies = AssemblyScanning.GetVPayAssemblies(typeof(Startup));

            var configuration = hostContext.Configuration;

            services.AddOptions();

            services
                .AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(vPayAssemblies))
                .AddVPayMemoryCache();

            services
                .Configure<Settings>(configuration.GetSection("Settings"))
                .AddTransient(cfg => cfg.GetService<IOptions<Settings>>().Value)

                .Configure<TransportConfig>(configuration.GetSection("RabbitQueue"))
                .AddTransient(cfg => cfg.GetService<IOptions<TransportConfig>>().Value);

            services.AddPolicies()
                .AddLoafApiClient(config => configuration.Bind("LoafApiHttpClientConfig", config))
                .AddDocSysGenerationApiClient(config => configuration.Bind("DocSysGenerationApiHttpClientConfig", config))
                .AddTransactionsApiClient(config => configuration.Bind("TransactionsApiHttpClientConfig", config))
                .AddConfigApiClient(config => configuration.Bind("ConfigApiHttpClientConfig", config));

            services
                .AddSql(config => configuration.Bind("OptumIntegrationDatabase", config))
                .WithTypeMappings(vPayAssemblies);

            services
                .AddWorkerHealthCheckPublisher()
                .AddHealthChecks()
                .AddCheck<SqlServerSystemHealthCheck>("sql-server")
                .AddDocSysGenerationApiHealthCheck()
                .AddTransactionsApiHealthCheck()
                .AddConfigApiHealthCheck();

            var rabbitConfig = new TransportConfig();
            configuration.Bind("RabbitQueue", rabbitConfig);

            services.UseMassTransit(rabbitConfig, opts =>
            {
                opts.AddConsumer<Nc835RecordNotificationConsumer, Nc835RecordNotificationConsumerDefinition>();
            });

            services.UseMassTransitActivityTracking();
            services.AddMassTransitHostedService();

            services.AddHostedService<LifetimeEventsHostedService>();
        }
    }
}
