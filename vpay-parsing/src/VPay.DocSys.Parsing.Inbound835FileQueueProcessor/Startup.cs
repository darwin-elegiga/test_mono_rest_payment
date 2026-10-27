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
using VPay.DocSys.Storage;
using VPay.Extensions.Ssh;
using VPay.Queuing.DependencyInjection;

namespace VPay.DocSys.Parsing.Inbound835FileQueueProcessor
{
    public static class Startup
    {
        public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IFileSystem, FileSystem>();
            services.AddSingleton<IDriverFileService, DriverFileService>();

            services.Configure<Edi835CreationMessageServiceSettings>(configuration.GetSection("Edi835CreationMessageServiceSettings"))
                .AddSingleton(cfg => cfg.GetService<IOptions<Edi835CreationMessageServiceSettings>>().Value);
            services.Configure<OtbDataServiceSettings>(configuration.GetSection("OtbDataServiceSettings"))
                .AddSingleton(cfg => cfg.GetService<IOptions<OtbDataServiceSettings>>().Value);
            services.Configure<Edi835ServiceSettings>(configuration.GetSection("Edi835ServiceSettings"))
                .AddSingleton(cfg => cfg.GetService<IOptions<Edi835ServiceSettings>>().Value);
            services.Configure<ApiSettings>(configuration.GetSection("ApiSettings"))
                .AddSingleton(cfg => cfg.GetService<IOptions<ApiSettings>>().Value);
            services.Configure<ApiRetrySettings>(configuration.GetSection("ApiRetrySettings"))
                .AddScoped(cfg => cfg.GetService<IOptionsSnapshot<ApiRetrySettings>>().Value);
            services.Configure<StatusFileSettings>(configuration.GetSection("StatusFile"))
                .AddScoped(cfg => cfg.GetService<IOptionsSnapshot<StatusFileSettings>>().Value);

            services.Configure<Inbound835FileServiceSettings>(configuration.GetSection("Inbound835FileServiceSettings"))
                .AddSingleton(cfg => cfg.GetService<IOptions<Inbound835FileServiceSettings>>().Value)
                .AddSingleton(cfg => cfg.GetService<IOptions<Inbound835FileServiceSettings>>().Value.AcknowledgmentFile)
                .AddSingleton(cfg => cfg.GetService<IOptions<Inbound835FileServiceSettings>>().Value.PaymentPostedFile);

            services.AddSingleton<Edi835ConfigurationCache>();
            services.AddSingleton<ISystemClock, SystemClock>();

            services.AddScoped<IDocsExtractorFactory, DocsExtractorFactory>();

            services.AddTransient<IAuxiliaryFileWriter, AuxiliaryFileWriter>();
            services.AddTransient<ITarService, TarService>();
            services.AddTransient<IEdi835TarService, Edi835TarService>();
            services.AddTransient<ITarExtractor, TarExtractor>();
            services.AddTransient<IStatusFileService, StatusFileService>();
            services.AddTransient<IStatusFilePathHelper, StatusFilePathHelper>();
            services.AddTransient<IStatusFileTracker, StatusFileTracker>();
            services.AddTransient<IDistributionMessageBuilder, DistributionMessageBuilder>();
            services.AddTransient<IOtbService, OtbService>();
            services.AddTransient<IDocumentTransactionMessageListBuilder, DocumentTransactionMessageListBuilder>();
            services.AddTransient<IDocSysConfigService, DocSysConfigService>();
            services.AddTransient<ICheckFileParser, CheckFileParser>();
            services.AddTransient<IOTBParserService, OTBParserService>();
            services.AddTransient<IOtbDataService, OtbDataService>();
            services.AddTransient<ISubFileParserService, SubFileParserService>();
            services.AddTransient<IInbound835FileParser, Inbound835FileParser>();
            services.AddTransient<IInbound835FileService, Inbound835FileService>();
            services.AddTransient<IInbound835AcknowledgmentFileProcessor, Inbound835AcknowledgmentFileProcessor>();
            services.AddTransient<IInbound835RemittanceAdviceFileProcessor, Inbound835RemittanceAdviceFileProcessor>();
            services.AddTransient<IInbound835PaymentPostedFileProcessor, Inbound835PaymentPostedFileProcessor>();
            services.AddTransient<IPaymentEngineConfirmationFileService, PaymentEngineConfirmationFileService>();
            services.AddTransient<ISftpService, SftpService>();

            services.ConfigureFileTransferServices(configuration);
            services.AddTransient<IPaymentEngineConfirmationFileSender, PaymentEngineConfirmationFileSender>();

            services.ConfigureQueueSettings<DocumentTransactionQueueSettings>(configuration, "DocumentQueue");
            services.ConfigureQueueSettings<DocumentDistributionQueueSettings>(configuration, "DistributionQueue");
            services.ConfigureQueueSettings<Inbound835FileQueueSettings>(configuration, "Inbound835FileQueue");
            services.ConfigureQueueSettings<Edi835GenerationQueueSettings>(configuration, "Edi835GenerationQueue");
            services.ConfigureQueueSettings<iSeriesPmbQueueSettings>(configuration, "iSeriesPmbQueue");

            services.ConfigureDataConnections(configuration);
            services.ConfigureHttpClients(configuration);
            services.ConfigureRefitApiDependencies(configuration.GetSection("ApiSettings").Get<ApiSettings>());

            services.AddVPaySshClientFactory();
            services.AddVPayQuorumQueuingServices();

            services.AddSingleton<MessageProcessor, MessageProcessor>();

            return services;
        }
    }
}
