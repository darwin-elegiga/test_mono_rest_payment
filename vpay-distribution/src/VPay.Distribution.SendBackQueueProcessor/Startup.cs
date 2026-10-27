using System;
using System.IO.Abstractions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using VPay.Distribution.Common;
using VPay.DocSys.Common;
using VPay.DocSys.Pdf.Tools;

namespace VPay.Distribution.SendBackQueueProcessor
{
    public static class Startup
    {
        public static IServiceCollection AddServices(this IServiceCollection services, HostBuilderContext hostContext)
        {
            var configuration = hostContext.Configuration;
            services.AddSingleton<IFileSystem>(new FileSystem());
            services.AddSingleton<ISftpClientFactory, SftpClientFactory>();
            services.AddSingleton<ISshClientFactory, SshClientFactory>();
            services.AddSingleton<ISshOperations, SshOperations>();
            services.AddSingleton<IProcessRunner, CommandLineProcessRunner>();
            services.AddSingleton<HttpRequestPolicyHolder, HttpRequestPolicyHolder>();
            services.AddSingleton<IDocSysConfigCache, DocSysConfigCache>();

            services.Configure<DocSysConfigCacheOptions>(configuration.GetSection("DocSysConfigCacheOptions"));
            services.AddSingleton(cfg => cfg.GetService<IOptions<DocSysConfigCacheOptions>>().Value);

            services.Configure<ApiSettings>(configuration.GetSection("ApiSettings"));
            services.AddSingleton(cfg => cfg.GetService<IOptions<ApiSettings>>().Value);

            services.AddTransient<IPdfTools, PdfTools>();
            services.AddTransient<ICustomSendBackProcessor, CustomSendBackProcessor>();
            services.AddTransient<ICustomSendBackService, CustomSendBackService>();
            services.AddTransient<ICustomSendBackDistributionQueueMessageProcessor, CustomSendBackDistributionQueueMessageProcessor>();
            services.AddTransient<IEventMessagePublisher, EventMessagePublisher>();
            services.AddTransient<IEdi835MessagePublisher, Edi835MessagePublisher>();

            services.ConfigureDataConnections(configuration);
            services.ConfigureQueueSettings(configuration);
            services.ConfigureHttpClients();
            services.AddSingleton<MessageProcessor>();
            services.AddHostedService<QueueProcessorService>();

            return services;
        }
    }
}
