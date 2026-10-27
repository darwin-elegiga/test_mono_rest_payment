using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using HealthChecks.Uris;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Refit;

namespace VPay.Optum.Http.Transactions
{
    public static class TransactionsApiConfigurationExtensions
    {
        /// <summary>
        /// Adds the Documents API client to the <paramref name="services"/> collection.
        /// </summary>
        /// <param name="services">The collection of services to add the client to.</param>
        /// <param name="configureHttpClient">Configuration properties.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddTransactionsApiClient(this IServiceCollection services, Action<TransactionsApiHttpClientConfig> configureHttpClient)
        {
            services.TryAddTransient<RemoveDoubleSlashDelegatingHandler>();

            var settings = new RefitSettings(new SystemTextJsonContentSerializer(new JsonSerializerOptions
            {
                NumberHandling =
                    JsonNumberHandling.AllowReadingFromString |
                    JsonNumberHandling.WriteAsString,
            }));

            settings.HttpMessageHandlerFactory = () => new HttpClientHandler() { ServerCertificateCustomValidationCallback = ((_, _, _, _) => true) };

            //for the health check
            services.AddHttpClient("UriTransactionsApi")
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (_, _, _, _) => true
                });

            return services
                .Configure(configureHttpClient)
                .AddTransient(cfg => cfg.GetService<IOptions<TransactionsApiHttpClientConfig>>()!.Value)
                .AddRefitClient<ITransactionsApi>(settings)
                .ConfigureHttpClient((sp, client) =>
                {
                    var clientConfig = sp.GetRequiredService<IOptions<TransactionsApiHttpClientConfig>>().Value;

                    client.BaseAddress = clientConfig.BaseAddress;
                    client.Timeout = clientConfig.Timeout;
                })
                .AddPolicyHandlerFromRegistry(PolicyNames.HttpRetry)
                .AddHttpMessageHandler<RemoveDoubleSlashDelegatingHandler>()
                .Services;
        }

        /// <summary>
        /// Adds a health check for the Documents API.
        /// </summary>
        /// <param name="healthChecksBuilder">The health check builder instance.</param>
        /// <returns>The health check builder instance.</returns>
        public static IHealthChecksBuilder AddTransactionsApiHealthCheck(this IHealthChecksBuilder healthChecksBuilder)
        {
            return healthChecksBuilder.Add(
                new HealthCheckRegistration(
                    "uri-transactions-api",
                    sp =>
                    {
                        var clientConfig = sp.GetRequiredService<IOptions<TransactionsApiHttpClientConfig>>().Value;
                        var uri = new Uri(clientConfig.BaseAddress, new Uri("web/services/About", UriKind.Relative));
                        var options = new UriHealthCheckOptions().AddUri(uri, opt => opt.UseTimeout(clientConfig.Timeout));

                        var httpFactory = sp.GetRequiredService<IHttpClientFactory>();
                        return new UriHealthCheck(options, () => httpFactory.CreateClient("UriTransactionsApi"));
                    },
                    default,
                    default
                ));
        }
    }
}
