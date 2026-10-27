using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using System.IO.Abstractions;

using VPay.Distribution.Common;
using VPay.DocSys.Common;

namespace VPay.Distribution.MailQueueProcessor
{
    public static class Startup
    {
        public static IServiceCollection AddServices(this IServiceCollection services, HostBuilderContext hostContext)
        {
            var configuration = hostContext.Configuration;

            services.Configure<ApiSettings>(configuration.GetSection("ApiSettings"));
            services.AddTransient(cfg => cfg.GetService<IOptions<ApiSettings>>().Value);

            services.AddSingleton<IFileSystem>(new FileSystem());
            services.AddSingleton<ISftpClientFactory, SftpClientFactory>();
            services.AddSingleton<ISshClientFactory, SshClientFactory>();
            services.AddSingleton<ISshOperations, SshOperations>();
            services.AddSingleton<IProcessRunner, CommandLineProcessRunner>();

            services.AddSingleton<IDocSysConfigCache, DocSysConfigCache>();
            services.Configure<DocSysConfigCacheOptions>(configuration.GetSection("DocSysConfigCacheOptions"));
            services.AddSingleton(cfg => cfg.GetService<IOptions<DocSysConfigCacheOptions>>().Value);

            services.AddSingleton<HttpRequestPolicyHolder, HttpRequestPolicyHolder>();
            services.AddTransient<IMailFileProcessor, MailFileProcessor>();
            services.AddTransient<IMailService, MailService>();
            services.AddTransient<IMailDistributionQueueMessageProcessor, MailDistributionQueueMessageProcessor>();
            services.Configure<Phaze04Settings>(configuration.GetSection("Phaze04Settings"));
            services.AddScoped(cfg => cfg.GetService<IOptionsSnapshot<Phaze04Settings>>().Value);
            services.AddTransient<IEventMessagePublisher, EventMessagePublisher>();
            services.AddTransient<IEdi835MessagePublisher, Edi835MessagePublisher>();
            services.AddTransient<IRedCardFaxFileProcessor, RedCardFaxFileProcessor>();
            
            services.ConfigureDataConnections(configuration);
            services.ConfigureQueueSettings(configuration);
            services.ConfigureHttpClients();
            services.AddSingleton<MessageProcessor>();
            services.AddHostedService<QueueProcessorService>();

            return services;
        }
    }
}
