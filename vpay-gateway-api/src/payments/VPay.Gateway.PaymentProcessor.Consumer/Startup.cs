using MassTransit;
using Microsoft.Extensions.Options;
using VPay.EventBus.Abstractions;
using VPay.EventBus.DependencyInjection;
using VPay.Gateway.Common;
using VPay.Gateway.Common.Guidewire;
using VPay.Gateway.Common.Kubera;
using VPay.Gateway.Common.PlatformConfig;
using VPay.Gateway.Data.Sql;
using VPay.Gateway.Data.Sql.DependencyInjection;
using VPay.Gateway.Data.Sql.Health;
using VPay.Gateway.PaymentProcessor.Consumer.Rabbit;
using VPay.MassTransit.Abstractions;
using VPay.MassTransit.DependencyInjection;

namespace VPay.Gateway.PaymentProcessor.Consumer;

public static class Startup
{
    public static void ConfigureServices(HostBuilderContext hostContext, IServiceCollection services)
    {
        //Recursively get assemblies that are referenced by the API assembly
        var vPayAssemblies = AssemblyScanning.GetVPayAssemblies(typeof(Startup));

        var configuration = hostContext.Configuration;

        services.AddOptions();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(vPayAssemblies));

        services.AddPolicies();
        services.AddVPayMemoryCache();

        services.AddGuidewire();

        services
            .AddPlatformConfigApiClient(config => configuration.Bind("PlatformConfigApiHttpClientConfig", config))
            .AddKuberaApiClient(config => configuration.Bind("KuberaApiHttpClientConfig", config));

        services
            .AddSocketListener(5001)
            .AddHealthChecks()
            .AddPlatformConfigHealthCheck()
            .AddKuberaApiHealthCheck()
            .AddCheck<SqlServerSystemHealthCheck>("gateway-sql-server");

        services
            .Configure<ConnectionConfig>(configuration.GetSection("GatewayDatabase"))
            .AddTransient(cfg => cfg.GetService<IOptions<ConnectionConfig>>().Value)
            .AddSql();

        ConfigureRabbitService(hostContext, services);
        ConfigureEventBusRabbit(hostContext, services);

        services.UseMassTransitActivityTracking();
        services.AddMassTransitHostedService();
        services.AddHostedService<Worker>();
    }

    private static void ConfigureRabbitService(HostBuilderContext hostContext, IServiceCollection services)
    {
        var rabbitConfig = new TransportConfig();
        hostContext.Configuration.Bind("RabbitQueue", rabbitConfig);

        services.UseMassTransit(rabbitConfig, x =>
        {
            x.AddConsumer<NotifyClientPaymentChangedConsumer, NotifyClientPaymentChangedConsumerDefinition>();
            x.AddConsumer<ProcessClientPaymentStatusRecordsConsumer, ProcessClientPaymentStatusRecordsConsumerDefinition>();
            x.AddConsumer<ProcessClientPaymentStatusRecordsFaultConsumer, ProcessClientPaymentStatusRecordsFaultConsumerDefinition>();
        });
    }

    private static void ConfigureEventBusRabbit(HostBuilderContext hostContext, IServiceCollection services)
    {
        var config = new EventBusConfig();
        hostContext.Configuration.Bind("EventBusRabbitQueue", config);

        services.UseEventBus(config, opts =>
        {
            opts.AddConsumer<TransactionChangedConsumer, TransactionChangedConsumerDefinition>();
        });
    }
}
