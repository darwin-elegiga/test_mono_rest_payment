using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Azure.Storage;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Azure;
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
using VPay.Optum.PaymentRouting.Common.Configuration;
using VPay.Optum.PaymentRouting.Common.Helpers;
using VPay.Optum.PaymentRouting.Common.Serialization;
using VPay.Optum.PaymentRouting.Data;
using VPay.Optum.PaymentRouting.Data.Sql.DependencyInjection;
using VPay.Optum.PaymentRouting.InboundPayment.Application.Executors;
using VPay.Optum.PaymentRouting.InboundPayment.Consumers.Consumers;
using VPay.Optum.PaymentRouting.InboundPayment.Consumers.Consumers.CheckCompletion;
using VPay.Optum.PaymentRouting.InboundPayment.Consumers.Consumers.PaymentRecord;
using VPay.Optum.PaymentRouting.InboundPayment.Consumers.Consumers.PaymentSummary;
using VPay.Optum.PaymentRouting.InboundPayment.Consumers.Consumers.VPayFunding;
using VPay.Optum.PaymentRouting.InboundPayment.Consumers.Consumers.VPayPayment;

namespace VPay.Optum.PaymentRouting.InboundPayment.Consumers
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

            services.AddTransient<ICheckBatchCompleteExecutor, CheckBatchCompleteExecutor>();

            services.AddHostedService<LifetimeEventsHostedService>();

            services
                .AddWorkerHealthCheckPublisher()
                .AddHealthChecks()
                .AddConfigApiHealthCheck();

            #region VPay Service Bus

            var rabbitConfig = new TransportConfig();
            configuration.Bind("VPayQueueSettings", rabbitConfig);

            // VPay Bus
            services.UseMassTransit(rabbitConfig, config =>
            {
                config.AddConsumer<PaymentSummaryConsumer, PaymentSummaryConsumerDefinition>();
                config.AddConsumer<PaymentRecordConsumer, PaymentRecordConsumerDefinition>();
                config.AddConsumer<CheckCompletionConsumer, CheckCompletionConsumerDefinition>();
            });

            #endregion VPay Service Bus

            #region Optum Service Bus

            services
                .Configure<OptumQueueNameSettings>(configuration.GetSection("OptumQueueNames"))
                .AddTransient(cfg => cfg.GetService<IOptions<OptumQueueNameSettings>>()!.Value);

            var optumConfig = new TransportConfig();
            configuration.Bind("OptumQueueSettings", optumConfig);

            // Optum Bus
            services.UseMassTransit<IOptumBus>(optumConfig, config =>
            {
                config.EndpointNameFormatter = VPayOptumEndpointNameFormatter.Instance;
                config.ConfigureAvroSerialization = new AvroMessageSerializerOptions
                {
                    UseAvroEncoding = true,
                    AvroMagicByte = BitConverter.ToInt16(new byte[] { 195, 1 }),
                    AvroSchemas = new Dictionary<string, long>
                    {
                        { nameof(VpayPayment), BitConverter.ToInt64(new byte[] { 55, 136, 126, 60, 12, 104, 48, 206 }) },
                        { nameof(VpayFunding), BitConverter.ToInt64(new byte[] { 207, 152, 239, 176, 59, 172, 98, 239 }) }
                    }
                };

                config.AddConsumer<VpayPaymentConsumer, VpayPaymentConsumerDefinition>();
                config.AddConsumer<VpayFundingConsumer, VpayFundingConsumerDefinition>();

                config.AllowResponseLogging = false;
            });

            #endregion Optum Service Bus

            // Configure Bus health checks before blob storage configuration
            services.UseMassTransitActivityTracking();
            services.AddMassTransitHostedService();

            #region Blob Storage

            services
                .Configure<StorageConfig>(configuration.GetSection(nameof(StorageConfig)))
                .AddTransient(cfg => cfg.GetService<IOptions<StorageConfig>>().Value);

            var storageConfig = new StorageConfig();
            configuration.Bind(nameof(StorageConfig), storageConfig);

            services.AddAzureClients(builder =>
            {
                if (storageConfig.BlobStorage == BlobStorageTechnology.Azure)
                {
                    var storageBlobUri = new Uri(storageConfig.BlobEndpoint);

                    builder.AddBlobServiceClient(storageBlobUri)
                        .ConfigureOptions(options => options.Retry.MaxRetries = 1);
                }
                else
                {
                    var storageCredentials = new StorageSharedKeyCredential(storageConfig.AccountName, storageConfig.AccountKey);
                    var storageBlobUri = new Uri(storageConfig.BlobEndpoint);

                    builder.AddBlobServiceClient(storageBlobUri, storageCredentials)
                        .ConfigureOptions(options => options.Retry.MaxRetries = 1);
                }
            });

            #endregion Blob Storage

            services
                .AddSql(config => configuration.Bind("PaymentRoutingDatabase", config))
                .WithTypeMappings(new[] { typeof(SqlDataConnection).Assembly });

       

            services
                .AddPolicies(config => configuration.Bind("Policies", config))
                .AddConfigApiClient(config => configuration.Bind("ConfigApiHttpClientConfig", config));
        }
    }
}
