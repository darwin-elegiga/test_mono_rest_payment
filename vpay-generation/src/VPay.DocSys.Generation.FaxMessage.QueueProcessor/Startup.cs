using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.IO.Abstractions;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Configs;
using VPay.DocSys.Generation.Extensions;
using VPay.DocSys.Generation.Models;
using VPay.DocSys.Pdf.Tools;
using VPay.DocSys.Storage;
using VPay.Queuing.DependencyInjection;


namespace VPay.DocSys.Generation.FaxMessage.QueueProcessor
{
    public static class Startup
    {
        public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions();

            services.Configure<ApiSettings>(configuration.GetSection("ApiSettings"))
                .AddTransient(cfg => cfg.GetService<IOptions<ApiSettings>>().Value);
            services.Configure<ProcessorSettings>(configuration.GetSection("ProcessorSettings"))
                .AddTransient(cfg => cfg.GetService<IOptions<ProcessorSettings>>().Value);
            services.Configure<StorageSettings>(configuration.GetSection("StorageSettings"))
                .AddTransient<IStorageSettings>(cfg => cfg.GetService<IOptions<StorageSettings>>().Value);
            services.Configure<FaxJobSettings>(configuration.GetSection("FaxJobSettings"))
              .AddTransient(cfg => cfg.GetService<IOptions<FaxJobSettings>>().Value);
            services.Configure<FaxBase>(configuration.GetSection("FaxBase"))
              .AddTransient(cfg => cfg.GetService<IOptions<FaxBase>>().Value);
            services.Configure<DocSysSettings>(configuration.GetSection("DocSysOperations"))
              .AddTransient(cfg => cfg.GetService<IOptions<DocSysSettings>>().Value);


            services.AddTransient<IPdfTools, PdfTools>();
            services.AddTransient<ITiffOperations, TiffOperations>();
            services.AddTransient<IEobXmlParser, EobXmlParser>();
            services.AddTransient<IFaxMessageService, FaxMessageService>();
            services.AddTransient<IWarrantyPdfCreator, WarrantyPdfCreator>();
            services.AddTransient<IFaxMessageXmlReader, FaxMessageXmlReader>();
            services.AddTransient<IFaxMessagePayloadProcessor, FaxMessagePayloadProcessor>();
            services.AddTransient<IFaxMessageQueueMessageProcessor, FaxMessageQueueMessageProcessor>();
            services.AddTransient<ITiffMessageQueuer, TiffMessageQueuer>();
            services.AddTransient<ITiffQueueMessageProcessor, TiffQueueMessageProcessor>();
            services.AddTransient<IFaxMessageErrorQueueMessageProcessor, FaxMessageErrorQueueMessageProcessor>();
            services.AddTransient<IEftFaxPdfCreator, EftFaxPdfCreator>();
            services.AddSingleton<FopBaseXsl>();
            services.AddSingleton<ReminderFaxBaseXsl>();
            services.AddSingleton<EftFaxFopXsl>();
            services.AddSingleton<IFileSystem, FileSystem>();
            services.AddTransient<IQueueService, QueueService>();
            services.AddSingleton<IProcessRunner, CommandLineProcessRunner>();
            services.AddVPayDocSysStorage();

            services.ConfigureDataConnections(configuration);
            services.ConfigureQueueSettings(configuration);
            services.ConfigureHttpClients(configuration);

            services.AddSingleton<MessageProcessor, MessageProcessor>();

            return services;
        }

    }
}
