using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using VPay.Data.Db2.Odbc;
using VPay.Data.Db2.Odbc.Helpers;
using VPay.DocSys.Common;
using VPay.DocSys.Data;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Common.Interfaces;
using VPay.DocSys.Generation.Data;
using VPay.DocSys.Generation.Models;
using VPay.DocSys.Storage;

namespace VPay.DocSys.Generation.Extensions
{
    public static class DependencyHelper
    {
        public static IServiceCollection ConfigureDataConnections(this IServiceCollection services, IConfiguration configuration)
        {
            // Register DocSysConnectionSettings and IDocSysConnection FIRST
            var docSysConnectionSettings = configuration.GetSection("DocSysConnection").Get<DocSysConnectionSettings>();
            if (docSysConnectionSettings != null)
            {
                services.AddScoped(_ => docSysConnectionSettings);
                // Register the IDocSysConnection factory from VPay.DocSys.Data NuGet package
                // This MUST come before registering repositories that depend on it
                services.AddDocSysDataServices(docSysConnectionSettings.GetConnectionString());
                services.AddVPayDocSysStorage();
            }

            // if "Db2" config section exists, register Db2OdbcConnection from VPay.Data
            var d2bConnectionConfig = configuration.GetSection("Db2").Get<OdbcConnectionConfig>();
            if (d2bConnectionConfig != null)
            {
                services.TryAddTransient(provider => d2bConnectionConfig);
                services.AddDb2OdbcConnection(d2bConnectionConfig.GetConnectionString());
            }

            // Register data repositories AFTER connections are registered
            if (docSysConnectionSettings != null)
            {
                services.AddTransient<IBundledFaxRepository, BundledFaxRepository>();
                services.AddTransient<IRedCardRepository, RedCardRepository>();
                services.AddTransient<IStpRemittanceRepository, StpRemittanceRepository>();
                services.AddTransient<IStpPaymentRepository, StpPaymentRepository>();
                services.AddTransient<IDocumentPaymentTransactionRepository, DocumentPaymentTransactionRepository>();
                services.AddTransient<IDb2TransactionRepository, Db2TransactionRepository>();
            }

            // if "OptumIntegrationConnection" section exists, register OptumIntegrationConnectionSettings
            var optumIntegConnectionSettings = configuration.GetSection("OptumIntegrationConnection").Get<OptumIntegrationConnectionSettings>();
            if (optumIntegConnectionSettings != null)
            {
                services.AddScoped(_ => optumIntegConnectionSettings);
            }

            return services;
        }

        public static IServiceCollection ConfigureQueueSettings(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddVPayQuorumQueuingServices();

            services.Configure<QueueConfigurations>(configuration)
                .AddScoped(cfg => cfg.GetService<IOptionsSnapshot<QueueConfigurations>>().Value);

            return services;
        }

        public static IServiceCollection ConfigureHttpClients(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHttpClient<IPaymentRoutingApiClient, PaymentRoutingApiClient>((sp, _) =>
            {
                _.BaseAddress = sp.GetService<IOptionsMonitor<ApiSettings>>()
                    .CurrentValue["PaymentRoutingApi"]
                    .GetUri();

                _.DefaultRequestHeaders.Accept.Clear();
                _.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            })
            .ConfigurePrimaryHttpMessageHandler(_ => new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip
            });

            services.AddHttpClient<IPlatformConfigurationApiClient, PlatformConfigurationApiClient>((sp, _) =>
            {
                _.BaseAddress = sp.GetService<IOptionsMonitor<ApiSettings>>()
                    .CurrentValue["PlatformConfigurationApi"]
                    .GetUri();

                _.DefaultRequestHeaders.Accept.Clear();
                _.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            })
            .ConfigurePrimaryHttpMessageHandler(_ => new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip
            });

            services.AddHttpClient<IGreenArrowApiClient, GreenArrowApiClient>((sp, _) =>
            {
                _.BaseAddress = sp.GetService<IOptionsMonitor<ApiSettings>>()
                    .CurrentValue["GreenArrowApi"]
                    .GetUri();

                _.DefaultRequestHeaders.Accept.Clear();
                _.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            })
            .ConfigurePrimaryHttpMessageHandler(_ => new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip
            });

            services.AddHttpClient<IParsingApiClient, ParsingApiClient>((sp, _) =>
            {
                _.BaseAddress = sp.GetService<IOptionsMonitor<ApiSettings>>()
                    .CurrentValue["ParsingApi"]
                    .GetUri();

                _.DefaultRequestHeaders.Accept.Clear();
                _.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            })
            .ConfigurePrimaryHttpMessageHandler(_ => new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip
            });

            // Configure Http Clients
            var apiSettings = configuration
                .GetSection("APISettings")
                .Get<ApiSettings>() ?? new ApiSettings();

            foreach (var setting in apiSettings.ServiceSettings)
            {
                services
                    // https://www.stevejgordon.co.uk/introduction-to-httpclientfactory-aspnetcore
                    .AddHttpClient(setting.ServiceName, _ =>
                    {
                        _.BaseAddress = setting.GetUri();
                        _.DefaultRequestHeaders.Accept.Clear();
                        _.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    })
                    // https://rehansaeed.com/optimally-configuring-asp-net-core-httpclientfactory/
                    .ConfigurePrimaryHttpMessageHandler(_ => new HttpClientHandler
                    {
                        AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip
                    });

                // only add if we have configuration
                //TODO: bring in FSSI Trac
                switch (setting.ServiceName)
                {
                    case FopApiClient.ServiceName:
                        services.AddTransient<IFopApiClient, FopApiClient>();
                        break;

                    case StaticFileApiClient.ServiceName:
                        services.AddTransient<IStaticFileApiClient, StaticFileApiClient>();
                        break;
                }
            }

            return services;
        }
    }
}
