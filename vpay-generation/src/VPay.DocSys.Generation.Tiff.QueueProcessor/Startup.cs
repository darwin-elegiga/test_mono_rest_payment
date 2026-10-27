using System.IO.Abstractions;
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


namespace VPay.DocSys.Generation.Tiff.QueueProcessor
{
    public static class Startup
    {
        public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions();

            services.Configure<DocSysSettings>(configuration.GetSection("DocSysOperations"))
                .AddScoped(cfg => cfg.GetService<IOptions<DocSysSettings>>().Value);
            services.Configure<ProcessorSettings>(configuration.GetSection("ProcessorSettings"))
                .AddTransient(cfg => cfg.GetService<IOptions<ProcessorSettings>>().Value);
            services.Configure<StorageSettings>(configuration.GetSection("StorageSettings"))
                .AddTransient<IStorageSettings>(cfg => cfg.GetService<IOptions<StorageSettings>>().Value);
            services.Configure<FaxJobSettings>(configuration.GetSection("FaxJobSettings"))
                .AddTransient(cfg => cfg.GetService<IOptions<FaxJobSettings>>().Value);
            services.Configure<FaxBase>(configuration.GetSection("FaxBase"))
                .AddTransient(cfg => cfg.GetService<IOptions<FaxBase>>().Value);

            services.AddSingleton<IFileSystem, FileSystem>();
            services.AddSingleton<IProcessRunner, CommandLineProcessRunner>();
            services.AddTransient<IPdfTools, PdfTools>();
            services.AddTransient<ITiffOperations, TiffOperations>();
            services.AddTransient<ITiffMessageQueuer, TiffMessageQueuer>();
            services.AddTransient<ITiffQueueMessageProcessor, TiffQueueMessageProcessor>();
            services.AddTransient<ITiffErrorQueueMessageProcessor, TiffErrorQueueMessageProcessor>();
            services.AddTransient<IQueueService, QueueService>();
            services.AddVPayDocSysStorage();


            services.ConfigureDataConnections(configuration);
            services.ConfigureQueueSettings(configuration);
            services.ConfigureHttpClients(configuration);
            services.AddSingleton<MessageProcessor, MessageProcessor>();

            return services;
        }

    }
}
