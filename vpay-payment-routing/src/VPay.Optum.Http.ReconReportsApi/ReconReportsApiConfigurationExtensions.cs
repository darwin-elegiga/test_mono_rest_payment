using System;
using System.Net.Http;
using HealthChecks.Uris;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Serialization;
using Polly;
using Refit;

namespace VPay.Optum.Http.ReconReportsApi
{
    public static class ReconReportsApiConfigurationExtensions
    {
        private const string ReconReportsApiHttpClientName = "ReconReportsApi";

        /// <summary>
        /// Adds the Recon Reports API client to the <paramref name="services"/> collection.
        /// </summary>
        /// <param name="services">The collection of services to add the client to.</param>
        /// <param name="configureHttpClient">Configuration properties.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddReconReportsApiClient(this IServiceCollection services, Action<ReconReportsApiHttpClientConfig> configureHttpClient)
        {
            services.TryAddTransient<RemoveDoubleSlashDelegatingHandler>();

            var settings = new RefitSettings
            {
                ContentSerializer = new NewtonsoftJsonContentSerializer(new Newtonsoft.Json.JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                }),
            };

            services.AddHttpClient(ReconReportsApiHttpClientName);

            return services
                .Configure(configureHttpClient)
                .AddTransient(cfg => cfg.GetService<IOptions<ReconReportsApiHttpClientConfig>>()!.Value)
                .AddRefitClient<IReconReportsApi>(settings)
                .ConfigureHttpClient((sp, client) =>
                {
                    var clientConfig = sp.GetRequiredService<IOptions<ReconReportsApiHttpClientConfig>>().Value;

                    client.BaseAddress = clientConfig.BaseAddress;
                    client.Timeout = clientConfig.Timeout;
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", clientConfig.ApiToken);
                })
                .AddPolicyHandlerFromRegistry((policyRegistry, message) =>
                    policyRegistry.Get<IAsyncPolicy<HttpResponseMessage>>(message.Method == HttpMethod.Get || message.Method == HttpMethod.Head ? PolicyNames.HttpRetry : PolicyNames.NoOp))
                .AddHttpMessageHandler<RemoveDoubleSlashDelegatingHandler>()
                .Services;
        }

        /// <summary>
        /// Adds a health check for the Recon Reports API.
        /// </summary>
        /// <param name="healthChecksBuilder">The health check builder instance.</param>
        /// <returns>The health check builder instance.</returns>
        public static IHealthChecksBuilder AddReconReportsApiHealthCheck(this IHealthChecksBuilder healthChecksBuilder)
        {
            return healthChecksBuilder.Add(
                new HealthCheckRegistration(
                    "recon-reports-api",
                    sp =>
                    {
                        var clientConfig = sp.GetRequiredService<IOptions<ReconReportsApiHttpClientConfig>>().Value;
                        var uri = new Uri(clientConfig.BaseAddress, new Uri("api/about", UriKind.Relative));
                        var options = new UriHealthCheckOptions().AddUri(uri, opt => opt.UseTimeout(clientConfig.Timeout));

                        var httpFactory = sp.GetRequiredService<IHttpClientFactory>();
                        return new UriHealthCheck(options, () => httpFactory.CreateClient(ReconReportsApiHttpClientName));
                    },
                    default,
                    default
                ));
        }
    }
}
