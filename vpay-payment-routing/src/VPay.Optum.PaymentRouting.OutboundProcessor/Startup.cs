using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.Security.Cryptography;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using VPay.Extensions.HealthChecks;
using VPay.FileTransfer.Messaging;
using VPay.FileTransfer.Messaging.Abstractions;
using VPay.FileTransfer.Messaging.Services;
using VPay.MassTransit.Abstractions;
using VPay.MassTransit.DependencyInjection;
using VPay.Optum.Http;
using VPay.Optum.Http.ConfigApi;
using VPay.Optum.Http.ReconReportsApi;
using VPay.Optum.PaymentRouting.Common.Helpers;
using VPay.Optum.PaymentRouting.Common.Serialization;
using VPay.Optum.PaymentRouting.Data;
using VPay.Optum.PaymentRouting.Data.Sql.DependencyInjection;
using VPay.Optum.PaymentRouting.Data.Sql.FinSeg;
using VPay.Optum.PaymentRouting.OutboundProcessor.Consumers.Outbound835;
using VPay.Optum.PaymentRouting.OutboundProcessor.Consumers.Outbound835File;
using VPay.Optum.PaymentRouting.OutboundProcessor.Consumers.OutboundFinSeg;
using VPay.Optum.PaymentRouting.OutboundProcessor.Consumers.OutboundFinSegFiles;
using VPay.Optum.PaymentRouting.OutboundProcessor.Consumers.OutboundFinSegTxtFiles;
using VPay.Optum.PaymentRouting.OutboundProcessor.FileTransfer;
using VPay.Optum.PaymentRouting.OutboundProcessor.Helpers;

namespace VPay.Optum.PaymentRouting.OutboundProcessor
{
    [ExcludeFromCodeCoverage]
    public static class Startup
    {
        public static void ConfigureServices(HostBuilderContext hostContext, IServiceCollection services)
        {
            //Recursively get assemblies that are referenced by the API assembly
            var vPayAssemblies = AssemblyScanning.GetVPayAssemblies(typeof(Startup));

            services
                .AddMediatR(vPayAssemblies);

            var configuration = hostContext.Configuration;

            services.AddOptions();

            services
                .AddVPayMemoryCache();

            services
                .AddSingleton<IFileSystem, FileSystem>()
                .AddSingleton<ISigningService, SigningService>();

            services.AddScoped<IClobConverter, ClobConverter>();

            services.AddScoped<IUserInfo, ConsumerServiceInfo>();

            services
                .AddTransient<IHashingService<SHA256CryptoServiceProvider>, HashingService<SHA256CryptoServiceProvider>>()
                .AddTransient<IFinSegFileWriter, FinSegFileWriter>();

            services.AddHostedService<LifetimeEventsHostedService>();

            services
                .AddPolicies(config => configuration.Bind("Policies", config))
                .AddConfigApiClient(config => configuration.Bind("ConfigApiHttpClientConfig", config))
                .AddReconReportsApiClient(config => configuration.Bind("ReconReportsApiHttpClientConfig", config));

            services
                .AddWorkerHealthCheckPublisher()
                .AddHealthChecks();

            var rabbitConfig = new TransportConfig();
            configuration.Bind("RabbitQueue", rabbitConfig);

            services.UseMassTransit(rabbitConfig, opts =>
            {
                opts.AddConsumer<Outbound835Consumer, Outbound835ConsumerDefinition>();
                opts.AddConsumer<Outbound835FileConsumer, Outbound835FileConsumerDefinition>();

                opts.AddConsumer<OutboundFinSegConsumer, OutboundFinSegConsumerDefinition>();
                opts.AddConsumer<OutboundFinSegFileConsumer, OutboundFinSegFileConsumerDefinition>();
                opts.AddConsumer<OutboundFinSegTxtFileConsumer, OutboundFinSegTxtFileConsumerDefinition>();
            });



            #region OptumWrap Service Bus

            services
              .Configure<TransportConfig>(configuration.GetSection("OptumWrapQueueSettings"))
              .AddTransient(cfg => cfg.GetService<IOptions<TransportConfig>>()?.Value);

            var optumWrapConfig = new TransportConfig();
            configuration.Bind("OptumWrapQueueSettings", optumWrapConfig);

            // Optum (OptumWrap) Bus
            services.UseMassTransit<IOptumWrapBus>(optumWrapConfig, config =>
            {
                config.AllowResponseLogging = false; //shouldn't need response logging
            });


            #endregion


            services.ConfigureFileTransferServices(configuration);
            services.UseMassTransitActivityTracking();
            services.AddMassTransitHostedService();

            services
                .AddSql(config => configuration.Bind("PaymentRoutingDatabase", config))
                .WithTypeMappings(new[] { typeof(SqlDataConnection).Assembly });

        }
    }
}
