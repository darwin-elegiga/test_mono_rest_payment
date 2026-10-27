using System.IO.Abstractions;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using VPay.Distribution;
using VPay.Distribution.Common;
using VPay.Distribution.Common.Extensions;
using VPay.Distribution.Common.Interfaces;
using VPay.Distribution.Common.Models;
using VPay.Distribution.Http.Extensions;
using VPay.DocSys.Common;
using VPay.DocSys.Data;
using VPay.DocSys.Storage;
using VPay.EventBus.Abstractions;
using VPay.EventBus.DependencyInjection;
using VPay.MassTransit.Abstractions;
using VPay.Queuing.DependencyInjection;

public static class DependencyHelper
{
    public static IServiceCollection ConfigureDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions();

        services.AddSingleton<IFileSystem>(new FileSystem());
        services.AddSingleton<ISftpClientFactory, SftpClientFactory>();
        services.AddSingleton<ISshClientFactory, SshClientFactory>();
        services.AddSingleton<ISshOperations, SshOperations>();
        services.AddSingleton<IProcessRunner, CommandLineProcessRunner>();
        services.AddSingleton<IDocSysConfigCache, DocSysConfigCache>();
        services.AddSingleton<HttpRequestPolicyHolder, HttpRequestPolicyHolder>();

        services.AddTransient<IFaxService, FaxService>();
        services.AddTransient<IFaxDistributionQueueMessageProcessor, FaxDistributionQueueMessageProcessor>();
        services.AddTransient<IMailDistributionQueueMessageProcessor, MailDistributionQueueMessageProcessor>();
        services.AddTransient<ICustomSendBackDistributionQueueMessageProcessor, CustomSendBackDistributionQueueMessageProcessor>();
        services.AddTransient<IDistributionRedCardQueueMessageProcessor, DistributionRedCardQueueMessageProcessor>();
        services.AddTransient<IEventMessagePublisher, EventMessagePublisher>();
        services.AddTransient<IEventDistributionQueueMessageProcessor, EventDistributionQueueMessageProcessor>();
        services.AddTransient<IDistributionStatusService, DistributionStatusService>();
        services.AddTransient<IPmbMessagePublisher, PmbMessagePublisher>();
        services.AddTransient<IRemittancePartnerNetworkApiClient, RemittancePartnerNetworkApiClient>();
        services.AddTransient<IRemittanceService, RemittanceService>();
        services.AddTransient<IStorageService, StorageService>();
        services.AddTransient<ISTPMessageQueueProcessor, STPDistributionMessageQueueProcessor>();
        services.AddTransient<IStpPaymentService, StpPaymentService>();
        services.AddTransient<IEdi835MessagePublisher, Edi835MessagePublisher>();

        var pmbConfig = new RabbitMqConfig();
        configuration.GetSection("PmbRabbitMq").Bind(pmbConfig);
        services.AddPmbMessaging(pmbConfig);

        services.Configure<AppSettings>(configuration.GetSection("AppSettings"));
        services.Configure<ApiSettings>(configuration.GetSection("ApiSettings"));
        services.Configure<DocSysConfigCacheOptions>(configuration.GetSection("DocSysConfigCacheOptions"));
        services.Configure<DelayConfig>(configuration);
        services.Configure<AuxiliaryScpSettings>(configuration.GetSection("AuxiliaryScpSettings"));

        services.AddScoped(cfg => cfg.GetService<IOptionsSnapshot<AppSettings>>().Value);
        services.AddScoped(cfg => cfg.GetService<IOptionsSnapshot<ApiSettings>>().Value);
        services.AddSingleton(cfg => cfg.GetService<IOptions<DocSysConfigCacheOptions>>().Value);
        services.AddScoped(cfg => cfg.GetService<IOptions<DelayConfig>>().Value);
        services.AddScoped(cfg => cfg.GetService<IOptionsSnapshot<AuxiliaryScpSettings>>().Value);

        services.ConfigureDataConnections(configuration);
        services.ConfigureEventBus(configuration);
        services.ConfigureQueueSettings(configuration);
        services.ConfigureHttpClients();
        services.ConfigureRefitHttpClients(configuration);

        return services;
    }

    public static IServiceCollection ConfigureDataConnections(this IServiceCollection services, IConfiguration configuration)
    {
        // if "DocSysConnection" section exists, register DocSysData Services from VPay.DocSys.Data
        var docSysConnectionSection = configuration.GetSection("DocSysConnection");
        if (docSysConnectionSection.Get<DocSysConnectionSettings>() is var docSysConnectionSettings && docSysConnectionSettings != null)
        {
            services.Configure<DocSysConnectionSettings>(docSysConnectionSection);
            services.AddDocSysDataServices();
        }

        // if "StorageSettings" section exists, register DocSysStorage Services from VPay.DocSys.Storage
        var storageSettingsSection = configuration.GetSection("StorageSettings");
        if (storageSettingsSection.Get<StorageSettings>() is var storageSettings && storageSettings != null)
        {
            services.Configure<StorageSettings>(storageSettingsSection);
            services.AddScoped<IStorageSettings>(cfg => cfg.GetService<IOptionsSnapshot<StorageSettings>>().Value);
            services.AddVPayDocSysStorage();
        }

        return services;
    }

    public static IServiceCollection ConfigureEventBus(this IServiceCollection services, IConfiguration configuration)
    {
        // if "EventBusConfig" section exists, setup event bus
        var eventBusConfig = configuration.GetSection("EventBusConfig").Get<EventBusConfig>();
        if (eventBusConfig != null)
        {
            services
                .UseEventBus(eventBusConfig)
                .AddEventBusHostedService();
        }

        return services;
    }

    public static IServiceCollection ConfigureQueueSettings(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddVPayQuorumQueuingServices();

        services.ConfigureQueueSettings<TiffQueueConfigurationSettings>(configuration, "TiffQueue");
        services.ConfigureQueueSettings<DistributionQueueConfigurationSettings>(configuration, "DistributionQueue");
        services.ConfigureQueueSettings<RedCardDistributionQueueConfigurationSettings>(configuration, "RedCardDistributionQueue");
        services.ConfigureQueueSettings<FaxDistributionQueueConfigurationSettings>(configuration, "FaxDistributionQueue");
        services.ConfigureQueueSettings<MailDistributionQueueConfigurationSettings>(configuration, "MailDistributionQueue");
        services.ConfigureQueueSettings<SendBackDistributionQueueConfigurationSettings>(configuration, "SendBackDistributionQueue");
        services.ConfigureQueueSettings<EventDistributionQueueConfigurationSettings>(configuration, "EventDistributionQueue");
        services.ConfigureQueueSettings<Edi835GenerationQueueConfigurationSettings>(configuration, "Edi835GenerationQueue");
        services.ConfigureQueueSettings<STPQueueConfigurationSettings>(configuration, "STPDistributionQueue");

        return services;
    }

    public static IServiceCollection ConfigureHttpClients(this IServiceCollection services)
    {
        services.AddHttpClient<IStorageApiClient, StorageApiClient>((sp, _) =>
        {
            _.BaseAddress = sp.GetService<IOptionsMonitor<ApiSettings>>()
                .CurrentValue["StorageApi"]
                .GetUri();

            _.DefaultRequestHeaders.Accept.Clear();
            _.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }).ConfigurePrimaryHttpMessageHandler(_ => new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip
        });

        services.AddHttpClient<IDocSysHttpClient, DocSysHttpClient>((provider, client) =>
        {
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }).ConfigurePrimaryHttpMessageHandler(_ => new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip
        });

        return services;
    }
}
