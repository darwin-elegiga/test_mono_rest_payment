using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using System.IO.Abstractions;

using VPay.Distribution.Common;
using VPay.Distribution.Common.Models;
using VPay.DocSys.Common;
using VPay.DocSys.Pdf.Tools;

namespace VPay.Distribution.FaxQueueProcessor
{
    public static class Startup
    {
        public static IServiceCollection AddServices(this IServiceCollection services, HostBuilderContext hostContext)
        {
            var configuration = hostContext.Configuration;
            services.Configure<ApiSettings>(configuration.GetSection("ApiSettings"));

            services.AddSingleton<IFileSystem>(new FileSystem());
            services.AddSingleton<ISftpClientFactory, SftpClientFactory>();
            services.AddSingleton<ISshClientFactory, SshClientFactory>();
            services.AddSingleton<ISshOperations, SshOperations>();
            services.AddSingleton<IProcessRunner, CommandLineProcessRunner>();
            
            services.AddSingleton<HttpRequestPolicyHolder, HttpRequestPolicyHolder>();
            services.AddTransient<IPdfTools, PdfTools>();
            services.Configure<ApiSettings>(configuration.GetSection("ApiSettings"));
            services.AddScoped(cfg => cfg.GetService<IOptionsSnapshot<ApiSettings>>().Value);
            services.AddTransient<IEventMessagePublisher, EventMessagePublisher>();
            services.AddTransient<IEdi835MessagePublisher, Edi835MessagePublisher>();

            services.AddTransient<IFaxFileProcessor, FaxFileProcessor>();
            services.AddTransient<IRedCardFaxFileProcessor, RedCardFaxFileProcessor>();
            services.AddTransient<IFaxService, FaxService>();
            services.AddTransient<IFaxMessagePublisher, FaxMessagePublisher>();
            services.AddTransient<IFaxDistributionQueueMessageProcessor, FaxDistributionQueueMessageProcessor>();
            services.AddTransient<IMailDistributionQueueMessageProcessor, MailDistributionQueueMessageProcessor>();
            services.AddTransient<IMailFileProcessor, MailFileProcessor>();
            services.AddTransient<IMailService, MailService>();
            services.Configure<Phaze04Settings>(configuration.GetSection("Phaze04Settings"));
            services.AddScoped(cfg => cfg.GetService<IOptionsSnapshot<Phaze04Settings>>().Value);

            services.AddSingleton<IDocSysConfigCache, DocSysConfigCache>();
            services.Configure<DocSysConfigCacheOptions>(configuration.GetSection("DocSysConfigCacheOptions"));
            services.AddSingleton(cfg => cfg.GetService<IOptions<DocSysConfigCacheOptions>>().Value);

            services.Configure<DelayConfig>(configuration);
            services.AddScoped(cfg => cfg.GetService<IOptions<DelayConfig>>().Value);
            services.ConfigureDataConnections(configuration);
            services.ConfigureQueueSettings(configuration);

            services.ConfigureHttpClients();
            services.AddSingleton<MessageProcessor>();
            services.AddHostedService<QueueProcessorService>();

            return services;
        }

    }
}
