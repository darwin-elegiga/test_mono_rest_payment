using System.IO.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using VPay.Distribution;
using VPay.Distribution.Common;
using VPay.Distribution.Common.Interfaces;
using VPay.Distribution.Http.Extensions;
using VPay.DocSys.Common;
using VPay.DocSys.Storage;
using VPay.MassTransit.Abstractions;

namespace Vpay.Distribution.STPMessageQueueProcessor
{
    public static class Startup
    {
        public static IServiceCollection AddServices(this IServiceCollection services, HostBuilderContext hostContext)
        {
            var configuration = hostContext.Configuration;
            services.AddOptions();

            services.Configure<ApiSettings>(configuration.GetSection("ApiSettings"))
                .AddTransient(cfg => cfg.GetService<IOptions<ApiSettings>>().Value);
            services.Configure<StorageSettings>(configuration.GetSection("StorageSettings"))
                .AddScoped<IStorageSettings>(cfg => cfg.GetService<IOptionsSnapshot<StorageSettings>>().Value);

            services.AddSingleton<IFileSystem>(new FileSystem());
            services.AddSingleton<ISftpClientFactory, SftpClientFactory>();
            services.AddSingleton<ISshClientFactory, SshClientFactory>();
            services.AddSingleton<ISshOperations, SshOperations>();
            services.AddSingleton<IProcessRunner, CommandLineProcessRunner>();
            services.AddTransient<IPmbMessagePublisher, PmbMessagePublisher>();
            services.AddTransient<IRemittancePartnerNetworkApiClient, RemittancePartnerNetworkApiClient>();
            services.AddTransient<IRemittanceService, RemittanceService>();
            services.AddTransient<IStorageManager, StorageManager>();
            services.AddTransient<IStorageService, StorageService>();
            services.AddTransient<ISTPMessageQueueProcessor, STPDistributionMessageQueueProcessor>();
            services.AddTransient<IStpPaymentService, StpPaymentService>();

            var pmbConfig = new RabbitMqConfig();
            configuration.GetSection("PmbRabbitMq").Bind(pmbConfig);
            services.AddPmbMessaging(pmbConfig);

            services.AddSingleton<HttpRequestPolicyHolder, HttpRequestPolicyHolder>();                                  
            services.ConfigureDataConnections(configuration);
            services.ConfigureQueueSettings(configuration);
            services.ConfigureHttpClients();
            services.ConfigureRefitHttpClients(configuration);
            services.AddSingleton<MessageProcessor>();
            services.AddHostedService<QueueProcessorService>();

            return services;

        }
    }
}
