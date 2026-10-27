using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.Reflection;
using System.Security.Cryptography;
using Azure.Storage;
using Azure.Storage.Blobs;
using FluentValidation.AspNetCore;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Swashbuckle.AspNetCore.SwaggerUI;
using VPay.AspNetCore.HealthChecks;
using VPay.AspNetCore.Logging;
using VPay.FileTransfer.Messaging;
using VPay.FileTransfer.Messaging.Abstractions;
using VPay.FileTransfer.Messaging.Services;
using VPay.MassTransit.Abstractions;
using VPay.MassTransit.DependencyInjection;
using VPay.MassTransit.Serialization;
using VPay.Optum.Http;
using VPay.Optum.Http.ConfigApi;
using VPay.Optum.Http.PreferencingApi;
using VPay.Optum.Http.ReconReportsApi;
using VPay.Optum.PaymentRouting.Api.Configuration;
using VPay.Optum.PaymentRouting.ApProcessor.Abstractions;
using VPay.Optum.PaymentRouting.ApProcessor.Services;
using VPay.Optum.PaymentRouting.Common.Configuration;
using VPay.Optum.PaymentRouting.Common.Helpers;
using VPay.Optum.PaymentRouting.Common.Interfaces;
using VPay.Optum.PaymentRouting.Common.Models;
using VPay.Optum.PaymentRouting.Common.Serialization;
using VPay.Optum.PaymentRouting.Data;
using VPay.Optum.PaymentRouting.Data.Sql.DependencyInjection;
using VPay.Optum.PaymentRouting.Data.Sql.FinSeg;
using VPay.Optum.PaymentRouting.OutboundProcessor.FileTransfer;
using VPay.Optum.PaymentRouting.OutboundProcessor.Helpers;

namespace VPay.Optum.PaymentRouting.Api
{
    using System.Net;
    using System.Net.Http;
    using Hellang.Middleware.ProblemDetails;
    using Hellang.Middleware.ProblemDetails.Mvc;
    using Microsoft.Extensions.Hosting;

    [ExcludeFromCodeCoverage]
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            Env = environment;
        }

        public IConfiguration Configuration { get; }
        public IWebHostEnvironment Env { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Force the runtime to utilize modern TLS protocols.
            // NOTE: Avoid disabling TLS certificate validation globally; trust the local dev cert instead.

            Assembly[] vPayAssemblies = AssemblyScanning.GetVPayAssemblies(typeof(Startup));

            services
                .AddMediatR(vPayAssemblies);

            services.AddSingleton<IFileSystem, FileSystem>();
            services.AddSingleton<ISystemClock, SystemClock>();

            services.AddScoped<IUserInfo, ConsumerServiceInfo>();
            services.AddScoped<IClobConverter, ClobConverter>();

            services
                .AddResponseCompression()
                .AddHttpContextAccessor()
                .ConfigureOptions<ProblemDetailsOptionsCustomSetup>()
                .AddProblemDetails()
                .ConfigureAuth(Configuration);

            services
                .AddPolicies(config => Configuration.Bind("Policies", config));

            #region AP Processor

            services
                .AddVPayMemoryCache()
                .AddConfigApiClient(config => Configuration.Bind("ConfigApiHttpClientConfig", config)) // Requires VPay Memory Cache for OtbBillingEntityDetailQueryHandler.
                .AddTransient<IApReapVPayToOptumConverter, ApReapVPayToOptumConverter>(); // Requires Platform Configuration API.

            services
                .AddPreferencingApiClient(config => Configuration.Bind("PreferencingApiHttpClientConfig", config));

            #endregion AP Processor

            #region Outbound Processor

            services
                .AddSingleton<ISigningService, SigningService>();

            services
                .AddTransient<IHashingService<SHA256CryptoServiceProvider>, HashingService<SHA256CryptoServiceProvider>>()
                .AddTransient<IFinSegFileWriter, FinSegFileWriter>();

            services.AddReconReportsApiClient(config => Configuration.Bind("ReconReportsApiHttpClientConfig", config));

            services.ConfigureFileTransferServices(Configuration);

            #endregion Outbound Processor

            #region OptumWrap Service Bus

            services
              .Configure<TransportConfig>(Configuration.GetSection("OptumWrapQueueSettings"))
              .AddTransient(cfg => cfg.GetService<IOptions<TransportConfig>>()?.Value);

            var optumWrapConfig = new TransportConfig();
            Configuration.Bind("OptumWrapQueueSettings", optumWrapConfig);

            // Optum (OptumWrap) Bus
            services.UseMassTransit<IOptumWrapBus>(optumWrapConfig, config =>
            {
                config.AllowResponseLogging = false; //shouldn't need response logging
            });

            #endregion OptumWrap Service Bus

            #region VPay Service Bus

            services
                .Configure<TransportConfig>(Configuration.GetSection("VPayQueueSettings"))
                .AddTransient(cfg => cfg.GetService<IOptions<TransportConfig>>()?.Value);

            var rabbitConfig = new TransportConfig();
            Configuration.Bind("VPayQueueSettings", rabbitConfig);

            // VPay Bus
            services.UseMassTransit(rabbitConfig, config =>
            {
                config.AllowResponseLogging = false; //shouldn't need response logging
            });

            #endregion VPay Service Bus

            #region Optum Service Bus

            services
                .Configure<OptumQueueNameSettings>(Configuration.GetSection("OptumQueueNames"))
                .AddTransient(cfg => cfg.GetService<IOptions<OptumQueueNameSettings>>()!.Value);

            var optumQueueConfig = new OptumQueueNameSettings();
            Configuration.Bind("OptumQueueNames", optumQueueConfig);

            var optumConfig = new TransportConfig();
            Configuration.Bind("OptumQueueSettings", optumConfig);

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

                config.AllowResponseLogging = false;
            });

            #endregion Optum Service Bus

            // Configure Bus health checks before blob storage configuration
            services.UseMassTransitActivityTracking();
            services.AddMassTransitHostedService();

            #region Blob Storage

            services
                .Configure<StorageConfig>(Configuration.GetSection(nameof(StorageConfig)))
                .AddTransient(cfg => cfg.GetService<IOptions<StorageConfig>>().Value);

            var storageConfig = new StorageConfig();
            Configuration.Bind(nameof(StorageConfig), storageConfig);

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

            #region Health Checks

            var endpointProtocol = storageConfig.BlobStorage == BlobStorageTechnology.Azure ? EndpointProtocol.Https : EndpointProtocol.Http;
            var optumQueueConnString = AzureHelper.BuildServiceBusConnectionString(optumConfig.PrimaryHostName, optumConfig.Username, optumConfig.Password);

            var healthCheckBuilder = services
                .AddHealthChecks()
                .AddConfigApiHealthCheck()
                .AddPreferencingApiHealthCheck()
                .AddReconReportsApiHealthCheck()
                .AddEdiFabricHealthCheck()
                .AddCheck<Data.Sql.Health.SqlServerSystemHealthCheck>("sql-server");

            if (storageConfig.BlobStorage == BlobStorageTechnology.Azure && !string.IsNullOrWhiteSpace(storageConfig.SharedAccessSignature))
            {
                var storageBlobConnString = AzureHelper.BuildStorageConnectionString(storageConfig.SharedAccessSignature.TrimStart('?'), EndpointType.BlobEndpoint, storageConfig.BlobEndpoint.TrimEnd('/'), true);
                healthCheckBuilder
                    .AddAzureBlobStorage(storageBlobConnString, name: storageConfig.AccountName);
            }
            else if ((storageConfig.BlobStorage == BlobStorageTechnology.Azure || storageConfig.BlobStorage == BlobStorageTechnology.Azurite) && !string.IsNullOrWhiteSpace(storageConfig.AccountKey))
            {
                var storageBlobConnString = AzureHelper.BuildStorageConnectionString(storageConfig.AccountName, storageConfig.AccountKey, EndpointType.BlobEndpoint, storageConfig.BlobEndpoint, endpointProtocol);
                healthCheckBuilder
                    .AddAzureBlobStorage(storageBlobConnString, name: storageConfig.AccountName);
            }
            if (optumConfig.ServiceBus == ServiceBusTechnology.Azure)
            {
                healthCheckBuilder
                    .AddAzureServiceBusQueue(optumQueueConnString, optumQueueConfig.AccountPayable, name: optumQueueConfig.AccountPayable)
                    .AddAzureServiceBusQueue(optumQueueConnString, optumQueueConfig.Funding, name: optumQueueConfig.Funding)
                    .AddAzureServiceBusQueue(optumQueueConnString, optumQueueConfig.Payment, name: optumQueueConfig.Payment);
            }

            #endregion Health Checks

            services
                .AddControllers(opt =>
                {
                    //opt.SetupSlugifyParametersRouteConvention();
                })
                .AddProblemDetailsConventions()
                .AddNewtonsoftJson(options =>
                {
                    options.UseCamelCasing(false);

                    options.SerializerSettings.Converters.Add(new StringEnumConverter());
                    options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                    options.SerializerSettings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
                })
                .AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<Startup>());

            services.EnableRequestMiddlewareValidationLogging();

            services
                .AddOptions()
                .AddEndpointsApiExplorer()
                .AddSwaggerGenService();

            services
                .AddSql(config => Configuration.Bind("PaymentRoutingDatabase", config))
                .WithTypeMappings(new[] { typeof(SqlDataConnection).Assembly });

            services.AddHostedService<LifetimeEventsHostedService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, BlobServiceClient blobServiceClient, StorageConfig storageConfig)
        {
            app
                .UseResponseCompression()
                .UseAuthentication()
                .UseRequestLogging()
                .UseProblemDetails()
                .UseSwagger();

            app
                .UseSwaggerUI(c =>
                {
                    const string Title = "Optum Payment Routing";
                    c.SwaggerEndpoint("/swagger/v1.0/swagger.json", $"{Title} - v1.0");
                    c.DocExpansion(DocExpansion.None);
                    c.SetupVPayDocumentStyles(Title);
                });

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/health", new DefaultHealthCheckOptions());
            });
        }
    }
}
