using System.IO.Abstractions;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Common.Interfaces;
using VPay.DocSys.Generation.Configs;
using VPay.DocSys.Generation.EmailMessage.QueueProcessor.Consumers;
using VPay.DocSys.Generation.EmailMessage.QueueProcessor.RabbitMq;
using VPay.DocSys.Generation.Extensions;
using VPay.DocSys.Generation.Models;
using VPay.DocSys.Pdf.Tools;
using VPay.DocSys.Storage;

namespace VPay.DocSys.Generation.EmailMessage.QueueProcessor;

public static class Startup
{
    public static void ConfigureServices(HostBuilderContext hostContext, IServiceCollection services)
    {
        var configuration = hostContext.Configuration;

        services.AddOptions();

        services
            .Configure<ApiSettings>(configuration.GetSection("ApiSettings"))
            .AddTransient(cfg => cfg.GetRequiredService<IOptions<ApiSettings>>().Value);

        services
            .Configure<StorageSettings>(configuration.GetSection("StorageSettings"))
            .AddTransient<IStorageSettings>(cfg => cfg.GetRequiredService<IOptions<StorageSettings>>().Value);

        services
            .Configure<DocSysSettings>(configuration.GetSection("DocSysOperations"))
            .AddTransient(cfg => cfg.GetRequiredService<IOptions<DocSysSettings>>().Value);

        services.AddTransient<IPdfTools, PdfTools>();
        services.AddTransient<IEmailMessageService, EmailMessageService>();
        services.AddTransient<IEmailMessageXmlReader, EmailMessageXmlReader>();
        services.AddTransient<IWarrantyPdfCreator, WarrantyPdfCreator>();
        services.AddTransient<IStaticFileApiClient, StaticFileApiClient>();
        services.AddTransient<IFopApiClient, FopApiClient>();
        services.AddSingleton<FopBaseXsl>();
        services.AddSingleton<IFileSystem, FileSystem>();

        services.AddVPayDocSysStorage();

        services.ConfigureHttpClients(configuration);

        services.SetupBus(configuration);

        services.AddHostedService<Worker>();
    }

    private static IServiceCollection SetupBus(this IServiceCollection services, IConfiguration configuration)
    {
        var rabbitConfig = new RabbitMqConfig();
        configuration.Bind("RabbitSettings", rabbitConfig);

        services.AddMassTransit(x =>
        {
            x.AddConsumer<CreateWarrantyEVPaygeConsumer, CreateWarrantyEVPaygeConsumerDefinition>();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitConfig);

                cfg.ConfigureMessageTopology();

                cfg.ConfigureEndpoints(context);
            });
            x.AddConfigureEndpointsCallback((name, cfg) =>
            {
                if (cfg is IRabbitMqReceiveEndpointConfigurator rmq)
                {
                    rmq.SetQuorumQueue(3);
                }
            });
        });

        return services;
    }
}
