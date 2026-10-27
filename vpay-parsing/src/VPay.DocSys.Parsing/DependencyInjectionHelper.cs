using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using VPay.Data.Db2.Odbc;
using VPay.Data.Db2.Odbc.Helpers;
using VPay.DocSys.Common;
using VPay.DocSys.Data;
using VPay.DocSys.Parsing.Common;
using VPay.DocSys.Parsing.Common.Extensions;
using VPay.DocSys.Parsing.Common.Interfaces;
using VPay.DocSys.Parsing.Http.Abstractions;
using VPay.DocSys.Parsing.Http.Extensions;
using VPay.DocSys.Storage;
using VPay.Extensions.Ssh;
using VPay.FileTransfer.Messaging;
using VPay.Queuing.DependencyInjection;

namespace VPay.DocSys.Parsing
{
    [ExcludeFromCodeCoverage]
    public static class DependencyInjectionHelper
    {
        public static IServiceCollection ConfigureDependencies(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions();

            services.Configure<ApiSettings>(configuration.GetSection("ApiSettings"))
                .AddScoped(cfg => cfg.GetService<IOptionsSnapshot<ApiSettings>>().Value);

            services.Configure<ApiRetrySettings>(configuration.GetSection("ApiRetrySettings"))
                .AddScoped(cfg => cfg.GetService<IOptionsSnapshot<ApiRetrySettings>>().Value);

            services.Configure<AuxiliaryScpSettings>(configuration.GetSection("AuxiliaryScpSettings"))
                .AddScoped(cfg => cfg.GetService<IOptionsSnapshot<AuxiliaryScpSettings>>().Value);

            services.Configure<RedCardDataScpSettings>(configuration.GetSection("RedCardDataScpSettings"))
                .AddScoped(cfg => cfg.GetService<IOptionsSnapshot<RedCardDataScpSettings>>().Value);

            services.Configure<NewDocSysArchive>(configuration.GetSection("NewDocSysArchive"))
                .AddScoped(cfg => cfg.GetService<IOptionsSnapshot<NewDocSysArchive>>().Value);

            services.Configure<StatusFileSettings>(configuration.GetSection("StatusFile"))
                .AddScoped(cfg => cfg.GetService<IOptionsSnapshot<StatusFileSettings>>().Value);

            services.Configure<OtbDataServiceSettings>(configuration.GetSection("OtbDataServiceSettings"))
                .AddScoped(cfg => cfg.GetService<IOptions<OtbDataServiceSettings>>().Value);

            services.Configure<Edi835ServiceSettings>(configuration.GetSection("Edi835ServiceSettings"))
                 .AddScoped(cfg => cfg.GetService<IOptionsSnapshot<Edi835ServiceSettings>>().Value);

            services.Configure<Inbound835FileServiceSettings>(configuration.GetSection("Inbound835FileServiceSettings"))
                .AddScoped(cfg => cfg.GetService<IOptionsSnapshot<Inbound835FileServiceSettings>>().Value)
                .AddScoped(cfg => cfg.GetService<IOptionsSnapshot<Inbound835FileServiceSettings>>().Value.AcknowledgmentFile)
                .AddScoped(cfg => cfg.GetService<IOptionsSnapshot<Inbound835FileServiceSettings>>().Value.PaymentPostedFile);

            services.AddSingleton<IFileSystem, FileSystem>();
            services.AddSingleton<IDriverFileService, DriverFileService>();
            services.AddSingleton<IProcessRunner, CommandLineProcessRunner>();
            services.AddSingleton<Edi835ConfigurationCache>();
            services.AddSingleton<ISystemClock, SystemClock>();

            services.AddScoped<IDocsExtractorFactory, DocsExtractorFactory>();

            services.AddTransient<IEdi835TarService, Edi835TarService>();
            services.AddTransient<ITarExtractor, TarExtractor>();
            services.AddTransient<IInbound835FileParser, Inbound835FileParser>();
            services.AddTransient<IInbound835FileService, Inbound835FileService>();
            services.AddTransient<IInbound835AcknowledgmentFileProcessor, Inbound835AcknowledgmentFileProcessor>();
            services.AddTransient<IInbound835RemittanceAdviceFileProcessor, Inbound835RemittanceAdviceFileProcessor>();
            services.AddTransient<IInbound835PaymentPostedFileProcessor, Inbound835PaymentPostedFileProcessor>();
            services.AddTransient<ISftpService, SftpService>();

            services.AddTransient<IPdfService, PdfService>();
            services.AddTransient<ITarService, TarService>();
            services.AddTransient<ITarFileService, TarFileService>();
            services.AddTransient<IAuxiliaryFileWriter, AuxiliaryFileWriter>();
            services.AddTransient<IOutboundRedCardDataDriverStatusWriter, OutboundRedCardDataDriverStatusWriter>();
            services.AddTransient<IOutboundRedCardDataDriverFileProcessor, OutboundRedCardDataDriverFileProcessor>();
            services.AddTransient<IDocumentTransactionMessageListBuilder, DocumentTransactionMessageListBuilder>();
            services.AddTransient<ICheckFileParser, CheckFileParser>();

            services.AddTransient<IDistributionMessageBuilder, DistributionMessageBuilder>();
            services.AddTransient<IDocSysConfigService, DocSysConfigService>();
            services.AddTransient<IDistributionMessageBuilder, DistributionMessageBuilder>();

            services.AddTransient<IStatusFileTracker, StatusFileTracker>();
            services.AddTransient<IStatusFilePathHelper, StatusFilePathHelper>();
            services.AddTransient<IStatusFileService, StatusFileService>();
            services.AddTransient<IDocumentTransactionMessageListBuilder, DocumentTransactionMessageListBuilder>();
            services.AddTransient<ICheckFileParser, CheckFileParser>();
            services.AddTransient<IOtbService, OtbService>();
            services.AddTransient<IOTBParserService, OTBParserService>();
            services.AddTransient<IOtbDataService, OtbDataService>();
            services.AddTransient<IXmlConfigParserService, XmlConfigParserService>();
            services.AddTransient<IOtbDataService, OtbDataService>();
            services.AddTransient<IArchiveRecordService, ArchiveRecordService>();

            services.AddTransient<ISubFileParserService, SubFileParserService>();
            services.AddTransient<IOutboundRedCardDataDriverFileService, OutboundRedCardDataDriverFileService>();
            services.AddTransient<IOutboundRedCardDataDriverStatusWriter, OutboundRedCardDataDriverStatusWriter>();
            services.AddTransient<IOutboundRedCardDataDriverFileProcessor, OutboundRedCardDataDriverFileProcessor>();
            services.AddTransient<IPaymentEngineConfirmationFileService, PaymentEngineConfirmationFileService>();
            services.AddTransient<IPaymentEngineConfirmationFileSender, PaymentEngineConfirmationFileSender>();

            services.AddTransient<IInventoryDriverFileService, InventoryDriverFileService>();
            services.AddTransient<IOtbDataService, OtbDataService>();
            services.AddTransient<IStpPaymentService, StpPaymentService>();

            services.ConfigureRefitApiDependencies(configuration.GetSection("ApiSettings").Get<ApiSettings>());
            services.ConfigureDataConnections(configuration);
            services.ConfigureQueueSettings(configuration);
            services.ConfigureHttpClients(configuration);

            services.AddVPaySshClientFactory();
            services.ConfigureFileTransferServices(configuration);

            return services;
        }

        public static IServiceCollection ConfigureDataConnections(this IServiceCollection services, IConfiguration configuration)
        {
            // if "Db2" config section exists, register Db2OdbcConnection from VPay.Data
            var odbcConnectionConfigSection = configuration.GetSection("Db2");
            if (odbcConnectionConfigSection.Get<OdbcConnectionConfig>() is var odbcConnectionConfig && odbcConnectionConfig is not null)
            {
                services.Configure<OdbcConnectionConfig>(odbcConnectionConfigSection);
                services.AddScoped(cfg => cfg.GetService<IOptionsSnapshot<OdbcConnectionConfig>>().Value);
                services.AddDb2OdbcConnection(odbcConnectionConfig.GetConnectionString());
            }

            // if "DocSysConnection" section exists, register DocSysDataServices from VPay.DocSys.Data
            var docSysConnectionSettingsSection = configuration.GetSection("DocSysConnection");
            if (docSysConnectionSettingsSection.Get<DocSysConnectionSettings>() is var docSysConnectionSettings && docSysConnectionSettings is not null)
            {
                services.Configure<DocSysConnectionSettings>(docSysConnectionSettingsSection);
                services.AddDocSysDataServices();
            }

            var storageSettingsSection = configuration.GetSection("StorageSettings");
            if (storageSettingsSection.Get<StorageSettings>() is var storageSettings && storageSettings is not null)
            {
                services.Configure<StorageSettings>(storageSettingsSection);
                services.AddTransient<IStorageSettings>(cfg => cfg.GetService<IOptionsSnapshot<StorageSettings>>().Value);
                services.AddVPayDocSysStorage();
            }

            return services;
        }

        public static IServiceCollection ConfigureFileTransferServices(this IServiceCollection services, IConfiguration configuration)
        {
            // add the service used to sign the file hash before sending the file to the File Transfer Service
            services
                .AddHashingService()
                .AddSigningService(options => configuration.Bind("SigningServiceOptions", options));

            // register the handler that will consume the FileReadyForTransferNotification
            services.ConfigureFileReadyForTransferNotificationPublisher(cfg =>
            {
                cfg.AddConsumer<PaymentEngineConfirmationFileReadyForTransferNotificationHandler>();
            });

            services.ConfigureFileTransferServiceBus(cfg =>
            {
                configuration.Bind("FileTransferServiceQueue", cfg);
            }, ac =>
            {
                ac.AddConsumer<PostPaymentEngineConfirmationEventMessageConsumer,
                        PostPaymentEngineConfirmationEventMessageConsumerDefinition>();
            });
            return services;
        }

        public static IServiceCollection ConfigureQueueSettings(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddVPayQuorumQueuingServices();

            services.ConfigureQueueSettings<StatusFileParsingQueueSettings>(configuration, "ParsingQueue");
            services.ConfigureQueueSettings<OutboundRedCardDataQueueSettings>(configuration, "OutboundRedCardDataQueue");
            services.ConfigureQueueSettings<InventoryDriverFileQueueSettings>(configuration, "InventoryDriverFileQueue");
            services.ConfigureQueueSettings<InventoryDriverRecordQueueSettings>(configuration, "InventoryDriverRecordQueue");
            services.ConfigureQueueSettings<DocumentTransactionQueueSettings>(configuration, "DocumentQueue");
            services.ConfigureQueueSettings<DocumentDistributionQueueSettings>(configuration, "DistributionQueue");
            services.ConfigureQueueSettings<RedCardDistributionQueueSettings>(configuration, "RedCardDistributionQueue");
            services.ConfigureQueueSettings<Inbound835FileQueueSettings>(configuration, "Inbound835FileQueue");
            services.ConfigureQueueSettings<iSeriesPmbQueueSettings>(configuration, "iSeriesPmbQueue");
            services.ConfigureQueueSettings<Edi835GenerationQueueSettings>(configuration, "Edi835GenerationQueue");

            return services;
        }

        public static IServiceCollection ConfigureHttpClients(this IServiceCollection services, IConfiguration configuration)
        {
            // Configure Http Clients
            var apiSettings = configuration
                .GetSection("APISettings")
                .Get<ApiSettings>();

            if (apiSettings != null)
            {
                foreach (var setting in apiSettings.ServiceSettings)
                {
                    // Add Platform Configuration API Http Client (only if we have configuration).
                    if (string.Equals(setting.ServiceName, PlatformConfigurationApiClient.ServiceName))
                    {
                        services.AddHttpClient<IPlatformConfigurationApiClient, PlatformConfigurationApiClient>((sp, _) =>
                        {
                            _.BaseAddress = setting.GetUri();

                            _.DefaultRequestHeaders.Accept.Clear();
                            _.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        })
                        .ConfigurePrimaryHttpMessageHandler(_ => new HttpClientHandler
                        {
                            AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip
                        });
                    }

                    services
                        // https://www.stevejgordon.co.uk/introduction-to-httpclientfactory-aspnetcore
                        .AddHttpClient(setting.ServiceName, _ =>
                        {
                            _.BaseAddress = setting.GetUri();

                            _.DefaultRequestHeaders.Accept.Clear();
                            _.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        })
                        // https://rehansaeed.com/optimally-configuring-asp-net-core-httpclientfactory/
                        .ConfigurePrimaryHttpMessageHandler(_ => new HttpClientHandler
                        {
                            AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip
                        });
                }

                // only add if we have configuration
                foreach (var serviceName in apiSettings.ServiceSettings.Select(_ => _.ServiceName))
                {
                    if (serviceName == DistributionApiClient.ServiceName)
                    {
                        services.AddTransient<IDistributionApiClient, DistributionApiClient>();
                    }
                }
            }

            return services;
        }
    }
}
