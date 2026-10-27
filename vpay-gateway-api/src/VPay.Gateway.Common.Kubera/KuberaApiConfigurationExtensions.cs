using HealthChecks.Uris;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Refit;
using VPay.Gateway.Common.Http;

namespace VPay.Gateway.Common.Kubera;

public static class KuberaApiConfigurationExtensions
{
    /// <summary>
    /// Adds the Kubera API client to the <paramref name="services"/> collection.
    /// </summary>
    /// <param name="services">The collection of services to add the client to.</param>
    /// <param name="configureHttpClient">Configuration properties.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddKuberaApiClient(this IServiceCollection services, Action<KuberaApiHttpClientConfig> configureHttpClient)
    {
        var settings = new RefitSettings
        {
            ContentSerializer = new NewtonsoftJsonContentSerializer(
                new Newtonsoft.Json.JsonSerializerSettings
                {
                    NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore
                })
        };

        // try to register the logging handler if it hasn't already been registered
        services.TryAddTransient<HttpLoggingHandler>();

        var httpClientBuilder = services
            .Configure<KuberaApiHttpClientConfig>(configureHttpClient)
            .AddTransient(cfg => cfg.GetRequiredService<IOptions<KuberaApiHttpClientConfig>>().Value)
            .AddRefitClient<IKuberaApi>(settings)
            .ConfigureHttpClient((sp, client) =>
            {
                var clientConfig = sp.GetRequiredService<IOptions<KuberaApiHttpClientConfig>>().Value;

                client.BaseAddress = clientConfig.BaseAddress;
                client.Timeout = clientConfig.Timeout;
            });

        // NOTE: Order of registration is important here.  The timeout policy should be registered *after* any retry policies
        httpClientBuilder
            .AddPolicyHandlerFromRegistry((policyRegistry, message) =>
                policyRegistry.Get<IAsyncPolicy<HttpResponseMessage>>(message.Method == HttpMethod.Get || message.Method == HttpMethod.Head ? PolicyNames.HttpRetry : PolicyNames.NoOp))
            .AddPolicyHandler((serviceProvider, message) =>
            {
                if (message.Method != HttpMethod.Get && message.Method != HttpMethod.Head)
                {
                    return Policy.NoOpAsync<HttpResponseMessage>();
                }

                var clientConfig = serviceProvider.GetRequiredService<IOptions<KuberaApiHttpClientConfig>>().Value;
                var logger = serviceProvider.GetRequiredService<ILogger<IKuberaApi>>();

                // calls to kubera should complete quickly (within a second or so).  Periodically a call
                // will take longer than that, but a retry will most likely fix it.  We'll use a timeout
                // policy here to enable a retry to kick in after a few seconds.
                return Policy.TimeoutAsync<HttpResponseMessage>(
                clientConfig.PerCallTimeout,
                onTimeoutAsync: (_, timeSpan, _) =>
                {
                    logger.LogWarning("Call to Kubera API resulted in a timeout after {Elapsed}.", timeSpan.TotalMilliseconds);
                    return Task.CompletedTask;
                });
            })
            .AddHttpMessageHandler<HttpLoggingHandler>();

        return services;
    }

    /// <summary>
    /// Adds a health check for the Kubera API.
    /// </summary>
    /// <param name="healthChecksBuilder">The health check builder instance.</param>
    /// <returns>The health check builder instance.</returns>
    public static IHealthChecksBuilder AddKuberaApiHealthCheck(this IHealthChecksBuilder healthChecksBuilder)
    {
        return healthChecksBuilder.Add(
            new HealthCheckRegistration(
                "uri-kubera-api",
                sp =>
                {
                    var clientConfig = sp.GetRequiredService<IOptions<KuberaApiHttpClientConfig>>().Value;
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
