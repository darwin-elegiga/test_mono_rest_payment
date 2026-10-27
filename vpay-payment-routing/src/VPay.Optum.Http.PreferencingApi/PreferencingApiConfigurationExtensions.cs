using System;
using System.Net.Http;
using System.Text.Json;
using HealthChecks.Uris;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Polly;
using Refit;

namespace VPay.Optum.Http.PreferencingApi
{
    public static class PreferencingApiConfigurationExtensions
    {
        private const string PreferencingApiHttpClientName = "UriPreferencingApi";

        /// <summary>
        /// Adds the Preferencing API client to the <paramref name="services"/> collection. Requires <seealso cref="AddVPayMemoryCache"/> be registered.
        /// </summary>
        /// <param name="services">The collection of services to add the client to.</param>
        /// <param name="configureHttpClient">Configuration properties.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddPreferencingApiClient(this IServiceCollection services, Action<PreferencingApiHttpClientConfig> configureHttpClient)
        {
            services.TryAddTransient<RemoveDoubleSlashDelegatingHandler>();

            var settings = new RefitSettings(new SystemTextJsonContentSerializer(new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));

            services.AddHttpClient(PreferencingApiHttpClientName);

            return services
                .Configure(configureHttpClient)
                .AddTransient(cfg => cfg.GetService<IOptions<PreferencingApiHttpClientConfig>>()!.Value)
                .AddRefitClient<IPreferencingApi>(settings)
                .ConfigureHttpClient((sp, client) =>
                {
                    var clientConfig = sp.GetRequiredService<IOptions<PreferencingApiHttpClientConfig>>().Value;

                    client.BaseAddress = clientConfig.BaseAddress;
                    client.Timeout = clientConfig.Timeout;
                })
                .AddPolicyHandlerFromRegistry((policyRegistry, message) =>
                    policyRegistry.Get<IAsyncPolicy<HttpResponseMessage>>(message.Method == HttpMethod.Get || message.Method == HttpMethod.Head ? PolicyNames.HttpRetry : PolicyNames.NoOp))
                .AddHttpMessageHandler<RemoveDoubleSlashDelegatingHandler>()
                .Services;
        }

        /// <summary>
        /// Adds a health check for the Config API.
        /// </summary>
        /// <param name="healthChecksBuilder">The health check builder instance.</param>
        /// <returns>The health check builder instance.</returns>
        public static IHealthChecksBuilder AddPreferencingApiHealthCheck(this IHealthChecksBuilder healthChecksBuilder)
        {
            return healthChecksBuilder.Add(
                new HealthCheckRegistration(
                    "uri-preferencing-api",
                    sp =>
                    {
                        var clientConfig = sp.GetRequiredService<IOptions<PreferencingApiHttpClientConfig>>().Value;
                        var uri = new Uri(clientConfig.BaseAddress, new Uri("api/about", UriKind.Relative));
                        var options = new UriHealthCheckOptions().AddUri(uri, opt => opt.UseTimeout(clientConfig.Timeout));

                        var httpFactory = sp.GetRequiredService<IHttpClientFactory>();
                        return new UriHealthCheck(options, () => httpFactory.CreateClient(PreferencingApiHttpClientName));
                    },
                    default,
                    default
                ));
        }


    }
}
