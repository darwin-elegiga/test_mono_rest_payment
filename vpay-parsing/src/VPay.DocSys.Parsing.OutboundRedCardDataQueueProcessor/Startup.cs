using System.IO.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using VPay.DocSys.Common;
using VPay.DocSys.Parsing.Common;
using VPay.DocSys.Parsing.Common.Extensions;
using VPay.DocSys.Parsing.Common.Interfaces;
using VPay.Queuing.DependencyInjection;

namespace VPay.DocSys.Parsing.OutboundRedCardDataQueueProcessor
{
    public static class Startup
    {
        public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions();

            services.Configure<ApiSettings>(configuration.GetSection("ApiSettings"))
                .AddScoped(cfg => cfg.GetService<IOptionsSnapshot<ApiSettings>>().Value);

            services.Configure<AuxiliaryScpSettings>(configuration.GetSection("AuxiliaryScpSettings"))
                .AddScoped(cfg => cfg.GetService<IOptionsSnapshot<AuxiliaryScpSettings>>().Value);

            services.Configure<RedCardDataScpSettings>(configuration.GetSection("RedCardDataScpSettings"))
                .AddScoped(cfg => cfg.GetService<IOptionsSnapshot<RedCardDataScpSettings>>().Value);

            services.AddSingleton<IFileSystem, FileSystem>();
            services.AddSingleton<IDriverFileService, DriverFileService>();
            services.AddTransient<IOutboundRedCardDataDriverStatusWriter, OutboundRedCardDataDriverStatusWriter>();
            services.AddTransient<IOutboundRedCardDataDriverFileProcessor, OutboundRedCardDataDriverFileProcessor>();
            services.AddTransient<IOutboundRedCardDataDriverFileService, OutboundRedCardDataDriverFileService>();
            services.AddTransient<ITarFileService, TarFileService>();

            services.AddVPayQuorumQueuingServices();

            services.ConfigureQueueSettings<OutboundRedCardDataQueueSettings>(configuration, "OutboundRedCardDataQueue");
            services.ConfigureQueueSettings<InventoryDriverFileQueueSettings>(configuration, "InventoryDriverFileQueue");
            services.ConfigureQueueSettings<InventoryDriverRecordQueueSettings>(configuration, "InventoryDriverRecordQueue");
            services.ConfigureQueueSettings<DocumentTransactionQueueSettings>(configuration, "DocumentQueue");
            services.ConfigureQueueSettings<DocumentDistributionQueueSettings>(configuration, "DistributionQueue");
            services.ConfigureQueueSettings<RedCardDistributionQueueSettings>(configuration, "RedCardDistributionQueue");

            services.ConfigureDataConnections(configuration);
            services.ConfigureHttpClients(configuration);
            services.AddSingleton<MessageProcessor, MessageProcessor>();

            return services;
        }
    }
}
