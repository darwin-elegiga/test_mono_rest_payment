using System;
using System.IO.Abstractions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using VPay.Distribution.Common;
using VPay.Distribution.Common.Models;
using VPay.DocSys.Common;

namespace VPay.Distribution.QueueProcessor
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
            services.AddTransient<IEventMessagePublisher, EventMessagePublisher>();
            services.AddTransient<IEdi835MessagePublisher, Edi835MessagePublisher>();
            services.Configure<DelayConfig>(configuration);
            services.AddScoped(cfg => cfg.GetService<IOptions<DelayConfig>>().Value);

            services.AddTransient<IDistributionQueueMessageProcessor, DistributionQueueMessageProcessor>();
            services.ConfigureDataConnections(configuration);
            services.ConfigureQueueSettings(configuration);
            services.ConfigureHttpClients();
            services.AddSingleton<MessageProcessor>();
            services.AddHostedService<QueueProcessorService>();

            return services;
        }
    }
}
