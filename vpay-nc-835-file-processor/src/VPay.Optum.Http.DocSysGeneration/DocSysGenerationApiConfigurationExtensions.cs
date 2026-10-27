using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using HealthChecks.Uris;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Refit;

namespace VPay.Optum.Http.DocSysGeneration
{
    public static class DocSysGenerationApiConfigurationExtensions
    {
        /// <summary>
        /// Adds the Documents API client to the <paramref name="services"/> collection.
        /// </summary>
        /// <param name="services">The collection of services to add the client to.</param>
        /// <param name="configureHttpClient">Configuration properties.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddDocSysGenerationApiClient(this IServiceCollection services, Action<DocSysGenerationApiHttpClientConfig> configureHttpClient)
        {
            var settings = new RefitSettings(new NewtonsoftJsonContentSerializer(new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() }));

            return services
                .Configure(configureHttpClient)
                .AddTransient(cfg => cfg.GetService<IOptions<DocSysGenerationApiHttpClientConfig>>()!.Value)
                .AddRefitClient<IDocSysGenerationApi>(settings)
                .ConfigureHttpClient((sp, client) =>
                {
                    var clientConfig = sp.GetRequiredService<IOptions<DocSysGenerationApiHttpClientConfig>>().Value;

                    client.BaseAddress = clientConfig.BaseAddress;
                    client.Timeout = clientConfig.Timeout;
                })
                .AddPolicyHandlerFromRegistry(PolicyNames.HttpRetry)
                .Services;
        }

        /// <summary>
        /// Adds a health check for the Documents API.
        /// </summary>
        /// <param name="healthChecksBuilder">The health check builder instance.</param>
        /// <returns>The health check builder instance.</returns>
        public static IHealthChecksBuilder AddDocSysGenerationApiHealthCheck(this IHealthChecksBuilder healthChecksBuilder)
        {
            return healthChecksBuilder.Add(
                new HealthCheckRegistration(
                    "uri-docsys-generation-api",
                    sp =>
                    {
                        var clientConfig = sp.GetRequiredService<IOptions<DocSysGenerationApiHttpClientConfig>>().Value;
                        var uri = new Uri(clientConfig.BaseAddress, new Uri("api/about", UriKind.Relative));
                        var options = new UriHealthCheckOptions().AddUri(uri, opt => opt.UseTimeout(clientConfig.Timeout));

                        var httpFactory = sp.GetRequiredService<IHttpClientFactory>();
                        return new UriHealthCheck(options, () => httpFactory.CreateClient());
                    },
                    default,
                    default
                ));
        }
    }
}
