using HealthChecks.Uris;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Refit;
using VPay.Gateway.Common.Http;

namespace VPay.Gateway.Common.RestPayments;

public static class RestPaymentApiConfigurationExtensions
{
    /// <summary>
    /// Adds the Void Payment API client to the <paramref name="services"/> collection.
    /// </summary>
    /// <param name="services">The collection of services to add the client to.</param>
    /// <param name="configureHttpClient">Action to set up configuration values.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddRestPaymentApiClient(this IServiceCollection services, Action<RestPaymentApiHttpClientConfig> configureHttpClient)
    {
        var settings = new RefitSettings(new NewtonsoftJsonContentSerializer(new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() }));

        // try to register the logging handler if it hasn't already been registered
        services.TryAddTransient<HttpLoggingHandler>();

        return services
            .Configure(configureHttpClient)
            .AddTransient(cfg => cfg.GetRequiredService<IOptions<RestPaymentApiHttpClientConfig>>().Value)
            .AddRefitClient<IRestPaymentApi>(settings)
            .ConfigureHttpClient((sp, client) =>
            {
                var clientConfig = sp.GetRequiredService<IOptions<RestPaymentApiHttpClientConfig>>().Value;

                client.BaseAddress = clientConfig.BaseAddress;
                client.Timeout = clientConfig.Timeout;
            })
            .AddHttpMessageHandler<HttpLoggingHandler>()
            .Services;
    }

    /// <summary>
    /// Adds a health check for the Void Payments API
    /// </summary>
    /// <param name="healthChecksBuilder">The health check builder instance.</param>
    /// <returns>The health check builder instance.</returns>
    public static IHealthChecksBuilder AddRestPaymentHealthCheck(this IHealthChecksBuilder healthChecksBuilder)
    {
        return healthChecksBuilder.Add(
            new HealthCheckRegistration(
                "uri-rest-payment-api",
                sp =>
                {
                    var clientConfig = sp.GetRequiredService<IOptions<RestPaymentApiHttpClientConfig>>().Value;
                    var uri = new Uri(clientConfig.BaseAddress!, new Uri("api/about", UriKind.Relative));
                    var options = new UriHealthCheckOptions().AddUri(uri, opt => opt.UseTimeout(clientConfig.Timeout));

                    var httpFactory = sp.GetRequiredService<IHttpClientFactory>();
                    return new UriHealthCheck(options, () => httpFactory.CreateClient());
                },
                default,
                default
            ));
    }
}
