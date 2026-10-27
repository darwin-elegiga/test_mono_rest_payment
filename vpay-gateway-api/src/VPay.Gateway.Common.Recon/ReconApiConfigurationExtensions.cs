using HealthChecks.Uris;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Serialization;
using Polly;
using Refit;
using VPay.Gateway.Common.Http;

namespace VPay.Gateway.Common.Recon;

public static class ReconApiConfigurationExtensions
{
    private const string ReconApiHttpClientConfigSectionName = "ReconApiHttpClientConfig";

    /// <summary>
    /// Adds the Recon API client to the <paramref name="services"/> collection.
    /// </summary>
    /// <param name="services">The collection of services to add the client to.</param>
    /// <param name="configuration">Configuration properties.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddReconApiClient(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = new RefitSettings
        {
            ContentSerializer = new NewtonsoftJsonContentSerializer(
                new Newtonsoft.Json.JsonSerializerSettings
                {
                    NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                }),
        };

        // try to register the logging handler if it hasn't already been registered
        services.TryAddTransient<HttpLoggingHandler>();

        return services
            .Configure<ReconApiHttpClientConfig>(configuration.GetSection(ReconApiHttpClientConfigSectionName))
            .AddTransient(cfg => cfg.GetService<IOptions<ReconApiHttpClientConfig>>().Value)
            .AddRefitClient<IReconApi>(settings)
            .ConfigureHttpClient((sp, client) =>
            {
                var clientConfig = sp.GetRequiredService<IOptions<ReconApiHttpClientConfig>>().Value;

                client.BaseAddress = clientConfig.BaseAddress;
                client.Timeout = clientConfig.Timeout;
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", clientConfig.ApiToken);
            })
            .AddPolicyHandlerFromRegistry((policyRegistry, message) =>
                policyRegistry.Get<IAsyncPolicy<HttpResponseMessage>>(message.Method == HttpMethod.Get || message.Method == HttpMethod.Head ? PolicyNames.HttpRetry : PolicyNames.NoOp))
            .AddHttpMessageHandler<HttpLoggingHandler>()
            .Services;
    }

    /// <summary>
    /// Adds a health check for the Recon API.
    /// </summary>
    /// <param name="healthChecksBuilder">The health check builder instance.</param>
    /// <returns>The health check builder instance.</returns>
    public static IHealthChecksBuilder AddReconApiHealthCheck(this IHealthChecksBuilder healthChecksBuilder)
    {
        return healthChecksBuilder.Add(
            new HealthCheckRegistration(
                "uri-recon-api",
                sp =>
                {
                    var clientConfig = sp.GetRequiredService<IOptions<ReconApiHttpClientConfig>>().Value;
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
