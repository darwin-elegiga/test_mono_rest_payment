using System.IO.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Extensions;
using VPay.DocSys.Generation.Models;
using VPay.DocSys.Storage;
using VPay.Queuing.DependencyInjection;


namespace VPay.DocSys.Generation.FaxMessage.ErrorQueueProcessor
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

            services.AddSingleton<FopBaseXsl>();
            services.AddTransient<IFaxMessageErrorQueueMessageProcessor, FaxMessageErrorQueueMessageProcessor>();
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
