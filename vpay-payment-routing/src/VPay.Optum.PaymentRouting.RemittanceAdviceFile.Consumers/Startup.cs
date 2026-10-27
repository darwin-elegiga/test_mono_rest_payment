using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using MassTransit;
using MediatR;
using VPay.Extensions.HealthChecks;
using VPay.MassTransit.Abstractions;
using VPay.MassTransit.DependencyInjection;
using VPay.Optum.Http;
using VPay.Optum.Http.ConfigApi;
using VPay.Optum.PaymentRouting.Common.Helpers;
using VPay.Optum.PaymentRouting.Data;
using VPay.Optum.PaymentRouting.Data.Sql.DependencyInjection;
using VPay.Optum.PaymentRouting.InboundPayment.Application.Executors;

namespace VPay.Optum.PaymentRouting.RemittanceAdviceFile.Consumers
{
    [ExcludeFromCodeCoverage]
    public static class Startup
    {
        public static void ConfigureServices(HostBuilderContext hostContext, IServiceCollection services)
        {
            //Recursively get assemblies that are referenced by the API assembly
            var vPayAssemblies = AssemblyScanning.GetVPayAssemblies(typeof(Startup));

            services.AddMediatR(vPayAssemblies);

            services.AddOptions();

            services.AddVPayMemoryCache();

            services.AddSingleton<IFileSystem, FileSystem>();

            services.AddScoped<IUserInfo, ConsumerServiceInfo>();

            services.AddHostedService<LifetimeEventsHostedService>();

            services
                .AddWorkerHealthCheckPublisher()
                .AddHealthChecks()
                .AddConfigApiHealthCheck();

            var configuration = hostContext.Configuration;

            services
                .AddPolicies(config => configuration.Bind("Policies", config))
                .AddConfigApiClient(config => configuration.Bind("ConfigApiHttpClientConfig", config));

            var rabbitConfig = new TransportConfig();

            configuration.Bind("VPayQueueSettings", rabbitConfig);

            services.UseMassTransit(rabbitConfig, opts =>
            {
                opts.AddConsumer<RemittanceAdviceFileConsumer, RemittanceAdviceFileConsumerDefinition>();
            });

            services.UseMassTransitActivityTracking();

            services.AddMassTransitHostedService();

            services
                .AddSql(config => configuration.Bind("PaymentRoutingDatabase", config))
                .WithTypeMappings(new[] { typeof(SqlDataConnection).Assembly });

            services.AddTransient<ICheckBatchCompleteExecutor, CheckBatchCompleteExecutor>();
        }
    }
}
