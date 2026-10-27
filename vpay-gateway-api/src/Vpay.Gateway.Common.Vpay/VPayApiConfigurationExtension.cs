using HealthChecks.Uris;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Serialization;
using Polly;
using Refit;
using VPay.Gateway.Common;
using VPay.Gateway.Common.Http;

namespace Vpay.Gateway.Common.Vpay;

public static class VPayApiConfigurationExtension
{
    private const string VPayApiConfigSectionName = "VPayApiHttpClientConfig";

    /// <summary>
    /// Adds the VPay API client to the <paramref name="services"/> collection.
    /// </summary>
    /// <param name="services">The collection of services to add the client to.</param>
    /// <param name="configuration">Configuration properties.</param>
    /// <returns>The name of the newly added client.</returns>
    public static IServiceCollection AddVPayApiClient(this IServiceCollection services, IConfiguration configuration)
    {
        RefitSettings settings = new()
        {
            ContentSerializer = new NewtonsoftJsonContentSerializer(
                new Newtonsoft.Json.JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                    NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore
                })
        };

        // try to register the logging handler if it hasn't already been registered
        services.TryAddTransient<HttpLoggingHandler>();

        return services
            .Configure<VPayApiHttpClientConfig>(configuration.GetSection(VPayApiConfigSectionName))
            .AddTransient(cfg => cfg.GetService<IOptions<VPayApiHttpClientConfig>>().Value)
            .AddRefitClient<IVPayApi>(settings)
            .ConfigureHttpClient((sp, client) =>
            {
                var clientConfig = sp.GetRequiredService<IOptions<VPayApiHttpClientConfig>>().Value;

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
    /// Adds a health check for the Preferencing API.
    /// </summary>
    /// <param name="healthChecksBuilder">The health check builder instance.</param>
    /// <returns>The health check builder instance.</returns>
    public static IHealthChecksBuilder AddVPayApiHealthCheck(this IHealthChecksBuilder healthChecksBuilder)
    {
        return healthChecksBuilder.Add(
            new HealthCheckRegistration(
                "vpay-360-api",
                sp =>
                {
                    var clientConfig = sp.GetRequiredService<IOptions<VPayApiHttpClientConfig>>().Value;
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
