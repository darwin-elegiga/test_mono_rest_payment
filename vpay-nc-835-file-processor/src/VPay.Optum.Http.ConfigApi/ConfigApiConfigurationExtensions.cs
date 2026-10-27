using System;
using System.Net.Http;
using System.Text.Json;
using HealthChecks.Uris;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Refit;

namespace VPay.Optum.Http.ConfigApi
{
    public static class ConfigApiConfigurationExtensions
    {
        private const string ConfigApiHttpClientName = "UriConfigApi";

        /// <summary>
        /// Adds the Documents API client to the <paramref name="services"/> collection.
        /// </summary>
        /// <param name="services">The collection of services to add the client to.</param>
        /// <param name="configureHttpClient">Configuration properties.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddConfigApiClient(this IServiceCollection services, Action<ConfigApiHttpClientConfig> configureHttpClient)
        {
            services.TryAddTransient<RemoveDoubleSlashDelegatingHandler>();

            var settings = new RefitSettings(new SystemTextJsonContentSerializer(new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));

            services.AddHttpClient(ConfigApiHttpClientName);

            return services
                .Configure(configureHttpClient)
                .AddTransient(cfg => cfg.GetService<IOptions<ConfigApiHttpClientConfig>>()!.Value)
                .AddRefitClient<IConfigApi>(settings)
                .ConfigureHttpClient((sp, client) =>
                {
                    var clientConfig = sp.GetRequiredService<IOptions<ConfigApiHttpClientConfig>>().Value;

                    client.BaseAddress = clientConfig.BaseAddress;
                    client.Timeout = clientConfig.Timeout;
                })
                .AddPolicyHandlerFromRegistry(PolicyNames.HttpRetry)
                .AddHttpMessageHandler<RemoveDoubleSlashDelegatingHandler>()
                .Services;
        }

        /// <summary>
        /// Adds a health check for the Config API.
        /// </summary>
        /// <param name="healthChecksBuilder">The health check builder instance.</param>
        /// <returns>The health check builder instance.</returns>
        public static IHealthChecksBuilder AddConfigApiHealthCheck(this IHealthChecksBuilder healthChecksBuilder)
        {
            return healthChecksBuilder.Add(
                new HealthCheckRegistration(
                    "uri-config-api",
                    sp =>
                    {
                        var clientConfig = sp.GetRequiredService<IOptions<ConfigApiHttpClientConfig>>().Value;
                        var uri = new Uri(clientConfig.BaseAddress, new Uri("api/about", UriKind.Relative));
                        var options = new UriHealthCheckOptions().AddUri(uri, opt => opt.UseTimeout(clientConfig.Timeout));

                        var httpFactory = sp.GetRequiredService<IHttpClientFactory>();
                        return new UriHealthCheck(options, () => httpFactory.CreateClient(ConfigApiHttpClientName));
                    },
                    default,
                    default
                ));
        }
    }
}
