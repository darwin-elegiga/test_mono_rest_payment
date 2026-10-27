using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using VPay.Extensions.HealthChecks;
using VPay.MassTransit.Abstractions;
using VPay.MassTransit.DependencyInjection;
using VPay.MassTransit.Serialization;
using VPay.Optum.Http;
using VPay.Optum.Http.ConfigApi;
using VPay.Optum.Http.PreferencingApi;
using VPay.Optum.PaymentRouting.ApProcessor.Abstractions;
using VPay.Optum.PaymentRouting.ApProcessor.Consumers;
using VPay.Optum.PaymentRouting.ApProcessor.Services;
using VPay.Optum.PaymentRouting.Common.Configuration;
using VPay.Optum.PaymentRouting.Common.Helpers;
using VPay.Optum.PaymentRouting.Common.Serialization;
using VPay.Optum.PaymentRouting.Data;
using VPay.Optum.PaymentRouting.Data.Sql.DependencyInjection;

namespace VPay.Optum.PaymentRouting.ApProcessor
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

            services.AddHostedService<LifetimeEventsHostedService>();

            services
                .AddPolicies(config => configuration.Bind("Policies", config))
                .AddConfigApiClient(config => configuration.Bind("ConfigApiHttpClientConfig", config))
                .AddPreferencingApiClient(config => configuration.Bind("PreferencingApiHttpClientConfig", config));

            services
                .AddWorkerHealthCheckPublisher()
                .AddHealthChecks();

            services.
                AddTransient<IApReapVPayToOptumConverter, ApReapVPayToOptumConverter>();
            services.
                AddTransient<IUserInfo, ConsumerServiceInfo>();

            var rabbitConfig = new TransportConfig();
            configuration.Bind("VPayQueueSettings", rabbitConfig);

            services.UseMassTransit(rabbitConfig, opts =>
            {
                opts.AddConsumer<ISeriesApReapMessageConsumer, ISeriesApReapMessageConsumerDefinition>();
                opts.AddConsumer<ApReapTrackingConsumer, ApReapTrackingConsumerDefinition>();
                opts.AddConsumer<ApReapPaymentConsumer, ApReapPaymentConsumerDefinition>();
                opts.AddConsumer<ApReapOutboundFileConsumer, ApReapOutboundFileConsumerDefinition>();
                opts.AddConsumer<ApReapOutboundApConsumer, ApReapOutboundApConsumerDefinition>();
            });

            services.UseMassTransitActivityTracking();
            services.AddMassTransitHostedService();

            services
                .AddSql(config => configuration.Bind("PaymentRoutingDatabase", config))
                .WithTypeMappings(new[] { typeof(SqlDataConnection).Assembly });

            ConfigureApReapMessageServiceBus(hostContext, services);
        }

        private static void ConfigureApReapMessageServiceBus(HostBuilderContext hostContext, IServiceCollection services)
        {
            services
                .Configure<OptumQueueNameSettings>(hostContext.Configuration.GetSection("OptumQueueNames"))
                .AddTransient(cfg => cfg.GetService<IOptions<OptumQueueNameSettings>>()!.Value);

            var transportConfig = new TransportConfig();
            hostContext.Configuration.Bind("OptumQueueSettings", transportConfig);

            // Optum Bus
            services.UseMassTransit<IOptumBus>(transportConfig, config =>
            {
                config.EndpointNameFormatter = VPayOptumEndpointNameFormatter.Instance;
                config.ConfigureAvroSerialization = new AvroMessageSerializerOptions
                {
                    UseAvroEncoding = true,
                    AvroMagicByte = BitConverter.ToInt16(new byte[] { 195, 1 }),
                    AvroSchemas = new Dictionary<string, long>()
                };
            });
        }
    }
}
