using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.IO.Abstractions;
using VPay.DocSys.Common;
using VPay.DocSys.Parsing.Common;
using VPay.DocSys.Parsing.Common.Extensions;
using VPay.DocSys.Parsing.Common.Interfaces;
using VPay.DocSys.Storage;
using VPay.Queuing.DependencyInjection;

namespace VPay.DocSys.Parsing.InventoryDriverQueueProcessor
{
    public static class Startup
    {
        public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions();

            services.Configure<ApiSettings>(configuration.GetSection("ApiSettings"))
                .AddSingleton(cfg => cfg.GetService<IOptionsSnapshot<ApiSettings>>().Value);

            services.Configure<NewDocSysArchive>(configuration.GetSection("NewDocSysArchive"))
                .AddScoped(cfg => cfg.GetService<IOptionsSnapshot<NewDocSysArchive>>().Value);

            services.Configure<RedCardDataScpSettings>(configuration.GetSection("RedCardDataScpSettings"))
              .AddSingleton(cfg => cfg.GetService<IOptionsSnapshot<RedCardDataScpSettings>>().Value);

            services.AddSingleton<IFileSystem, FileSystem>();
            services.AddSingleton<IDriverFileService, DriverFileService>();
            services.AddTransient<IDistributionMessageBuilder, DistributionMessageBuilder>();
            services.AddTransient<IOutboundRedCardDataDriverStatusWriter, OutboundRedCardDataDriverStatusWriter>();
            services.AddTransient<IOutboundRedCardDataDriverFileProcessor, OutboundRedCardDataDriverFileProcessor>();
            services.AddTransient<IOutboundRedCardDataDriverFileService, OutboundRedCardDataDriverFileService>();
            services.AddTransient<IInventoryDriverFileService, InventoryDriverFileService>();

            services.AddVPayQuorumQueuingServices();
            services.AddTransient<IPdfService, PdfService>();
            services.AddTransient<ITarService, TarService>();
            services.AddTransient<ITarFileService, TarFileService>();
            services.ConfigureQueueSettings<StatusFileParsingQueueSettings>(configuration, "ParsingQueue");
            services.AddTransient<IAuxiliaryFileWriter, AuxiliaryFileWriter>();
            services.ConfigureQueueSettings<OutboundRedCardDataQueueSettings>(configuration, "OutboundRedCardDataQueue");
            services.ConfigureQueueSettings<InventoryDriverFileQueueSettings>(configuration, "InventoryDriverFileQueue");
            services.ConfigureQueueSettings<InventoryDriverRecordQueueSettings>(configuration, "InventoryDriverRecordQueue");
            services.ConfigureQueueSettings<DocumentTransactionQueueSettings>(configuration, "DocumentQueue");
            services.ConfigureQueueSettings<DocumentDistributionQueueSettings>(configuration, "DistributionQueue");
            services.ConfigureQueueSettings<RedCardDistributionQueueSettings>(configuration, "RedCardDistributionQueue");
            services.ConfigureDataConnections(configuration);
            services.ConfigureHttpClients(configuration);
            services.AddSingleton<MessageProcessor, MessageProcessor>();
            services.AddSingleton<Edi835ConfigurationCache>();

            return services;
        }
    }
}
