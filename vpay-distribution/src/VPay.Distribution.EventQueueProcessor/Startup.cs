using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using System.IO.Abstractions;

using VPay.Distribution.Common;
using VPay.DocSys.Common;

namespace VPay.Distribution.EventQueueProcessor
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
            services.AddTransient<IEventDistributionQueueMessageProcessor, EventDistributionQueueMessageProcessor>();


            services.ConfigureEventBus(configuration);
            services.AddSingleton<HttpRequestPolicyHolder, HttpRequestPolicyHolder>();
            services.ConfigureHttpClients();
            services.AddSingleton<MessageProcessor>();
            services.AddHostedService<QueueProcessorService>();

            return services;
        }
    }
}
