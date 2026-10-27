using System.IO.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using VPay.DocSys.Common;
using VPay.DocSys.Parsing.Common;
using VPay.DocSys.Parsing.Common.Extensions;
using VPay.DocSys.Parsing.Common.Interfaces;
using VPay.DocSys.Parsing.Http.Abstractions;
using VPay.DocSys.Parsing.Http.Extensions;
using VPay.Queuing.DependencyInjection;

namespace VPay.DocSys.Parsing.QueueProcessor.Consolidated
{
    public static class Startup
    {
        public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<StatusFileSettings>(configuration.GetSection("StatusFile"))
              .AddScoped(cfg => cfg.GetService<IOptionsSnapshot<StatusFileSettings>>().Value);
            services.Configure<OtbDataServiceSettings>(configuration.GetSection("OtbDataServiceSettings"))
              .AddSingleton(cfg => cfg.GetService<IOptions<OtbDataServiceSettings>>().Value);
            services.Configure<Edi835ServiceSettings>(configuration.GetSection("Edi835ServiceSettings"))
                .AddSingleton(cfg => cfg.GetService<IOptions<Edi835ServiceSettings>>().Value);
            services.AddSingleton<IFileSystem, FileSystem>();
            services.AddSingleton<IDriverFileService, DriverFileService>();
            services.Configure<Edi835CreationMessageServiceSettings>(configuration.GetSection("Edi835CreationMessageServiceSettings"))
                .AddSingleton(cfg => cfg.GetService<IOptions<Edi835CreationMessageServiceSettings>>().Value);
            services.AddSingleton<Edi835ConfigurationCache>();

            services.AddScoped<IDocsExtractorFactory, DocsExtractorFactory>();

            services.AddTransient<ITarService, TarService>();
            services.AddTransient<IEdi835TarService, Edi835TarService>();
            services.AddTransient<ITarExtractor, TarExtractor>();
            services.AddTransient<IStatusFilePathHelper, StatusFilePathHelper>();
            services.AddTransient<IStatusFileService, StatusFileService>();
            services.AddTransient<IDistributionMessageBuilder, DistributionMessageBuilder>();
            services.AddTransient<IDocSysConfigService, DocSysConfigService>();

            services.AddVPayQuorumQueuingServices();
            services.AddTransient<IAuxiliaryFileWriter, AuxiliaryFileWriter>();
            services.AddTransient<IDocumentTransactionMessageListBuilder, DocumentTransactionMessageListBuilder>();
            services.AddTransient<IOTBParserService, OTBParserService>();
            services.AddTransient<IOtbDataService, OtbDataService>();
            services.AddTransient<IOtbService, OtbService>();
            services.AddTransient<ICheckFileParser, CheckFileParser>();
            services.AddTransient<IStatusFileTracker, StatusFileTracker>();
            services.AddTransient<ISubFileParserService, SubFileParserService>();
            services.AddTransient<IPaymentEngineConfirmationFileService, PaymentEngineConfirmationFileService>();
            services.AddSingleton<ISystemClock, SystemClock>();
            services.ConfigureQueueSettings<StatusFileParsingQueueSettings>(configuration, "ParsingQueue");
            services.ConfigureQueueSettings<DocumentTransactionQueueSettings>(configuration, "DocumentQueue");
            services.ConfigureQueueSettings<DocumentDistributionQueueSettings>(configuration, "DistributionQueue");
            services.ConfigureDataConnections(configuration);
            services.ConfigureRefitApiDependencies(configuration.GetSection("ApiSettings").Get<ApiSettings>());

            services.Configure<ApiSettings>(configuration.GetSection("ApiSettings"))
                .AddScoped(cfg => cfg.GetService<IOptionsSnapshot<ApiSettings>>().Value);
            services.Configure<ApiRetrySettings>(configuration.GetSection("ApiRetrySettings"))
                .AddScoped(cfg => cfg.GetService<IOptionsSnapshot<ApiRetrySettings>>().Value);

            services.ConfigureFileTransferServices(configuration);
            services.AddTransient<IPaymentEngineConfirmationFileSender, PaymentEngineConfirmationFileSender>();

            services.ConfigureHttpClients(configuration);
            services.AddSingleton<MessageProcessor, MessageProcessor>();

            return services;
        }
    }
}
