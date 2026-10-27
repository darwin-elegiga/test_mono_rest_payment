using MassTransit;
using Newtonsoft.Json;
using VPay.Gateway.ClientProcessor.Configurations;
using VPay.Gateway.ClientProcessor.Rabbit;
using VPay.Gateway.Common;
using VPay.Gateway.Common.Guidewire;
using VPay.Gateway.Common.PlatformConfig;
using VPay.Gateway.Data.Sql.DependencyInjection;
using VPay.MassTransit.Abstractions;
using VPay.MassTransit.DependencyInjection;

namespace VPay.Gateway.ClientProcessor;

public static class Startup
{
    public static void ConfigureServices(HostBuilderContext hostContext, IServiceCollection services)
    {
        var vPayAssemblies = AssemblyScanning.GetVPayAssemblies(typeof(Startup));

        var configuration = hostContext.Configuration;

        services.AddOptions();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(vPayAssemblies));

        services.AddPolicies();
        services.AddVPayMemoryCache();

        services.AddGuidewire();

        services
            .AddPepClaimsApiClient(configuration)
            .AddPlatformConfigApiClient(config => configuration.Bind("PlatformConfigApiHttpClientConfig", config));

        services
            .AddSocketListener(5001)
            .AddHealthChecks()
            .AddPlatformConfigHealthCheck();

        services
            .AddSql()
            .WithConnectionConfiguration(config => configuration.Bind("GatewayDatabase", config))
            .WithTypeMappings(vPayAssemblies);

        services.UseMassTransitActivityTracking();
        services.AddMassTransitHostedService();

        ConfigureRabbit(hostContext, services);
        ConfigurePepKuberaRabbit(hostContext, services);

        services.AddHostedService<Worker>();
    }

    private static void ConfigureRabbit(HostBuilderContext hostContext, IServiceCollection services)
    {
        var rabbitConfig = new TransportConfig();
        hostContext.Configuration.Bind("RabbitQueue", rabbitConfig);

        services.UseMassTransit(rabbitConfig, x =>
        {
            x.AddConsumer<SendGuidewireMessageConsumer, SendGuidewireMessageConsumerDefinition>();
            x.AddConsumer<ProcessPepClaimRecordConsumer, ProcessPepClaimRecordConsumerDefinition>();
            x.AddConsumer<ProcessEnrollmentStatusRecordsConsumer, ProcessEnrollmentStatusRecordsConsumerDefinition>();

            x.AddConsumer<SendGuidewireMessageFaultConsumer, SendGuidewireMessageFaultConsumerDefinition>();

            x.ConfigureJsonDeserializer = ConfigureSerializer;
            x.ConfigureJsonSerializer = ConfigureSerializer;
        });

        static JsonSerializerSettings ConfigureSerializer(JsonSerializerSettings settings)
        {
            settings.Converters.Add(new SmartEnumValueConverter());
            return settings;
        }
    }

    private static void ConfigurePepKuberaRabbit(HostBuilderContext hostContext, IServiceCollection services)
    {
        var pepKuberaRabbitConfig = new TransportConfig();
        hostContext.Configuration.Bind("PepKuberaRabbitQueue", pepKuberaRabbitConfig);

        services.UseMassTransit<IPepKuberaBus>(pepKuberaRabbitConfig, configure =>
        {
            configure.AddConsumer<IncomingMessageConsumer, IncomingMessageConsumerDefinition>();
        });
    }
}
