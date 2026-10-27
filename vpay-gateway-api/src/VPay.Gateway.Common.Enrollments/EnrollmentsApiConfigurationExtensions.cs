using System.Net.Http.Headers;
using HealthChecks.Uris;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Polly;
using Refit;
using VPay.Gateway.Common;
using VPay.Gateway.Common.Http;

namespace VPay.Gateway.Common.Enrollments;

public static class EnrollmentsApiConfigurationExtensions
{
    private const string EnrollmentsApiHttpClientConfigSectionName = "EnrollmentApiHttpClientConfig";

    /// <summary>
    /// Adds the Enrollments API client to the <paramref name="services"/> collection.
    /// </summary>
    /// <param name="services">The collection of services to add the client to.</param>
    /// <param name="configuration">Configuration properties.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddEnrollmentsApiClient(this IServiceCollection services, IConfiguration configuration)
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

        return services
            .Configure<EnrollmentApiHttpClientConfig>(configuration.GetSection(EnrollmentsApiHttpClientConfigSectionName))
            .AddTransient(cfg => cfg.GetRequiredService<IOptions<EnrollmentApiHttpClientConfig>>().Value)
            .AddRefitClient<IEnrollmentsApi>(settings)
            .ConfigureHttpClient((sp, client) =>
            {
                var clientConfig = sp.GetRequiredService<IOptions<EnrollmentApiHttpClientConfig>>().Value;

                client.BaseAddress = clientConfig.BaseAddress;
                client.Timeout = clientConfig.Timeout;
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", clientConfig.ApiToken);
            })
            .AddPolicyHandlerFromRegistry((policyRegistry, message) =>
                policyRegistry.Get<IAsyncPolicy<HttpResponseMessage>>(message.Method == HttpMethod.Get || message.Method == HttpMethod.Head ? PolicyNames.HttpRetry : PolicyNames.NoOp))
            .AddHttpMessageHandler<HttpLoggingHandler>()
            .Services;
    }

    /// <summary>
    /// Adds a health check for the Enrollments API.
    /// </summary>
    /// <param name="healthChecksBuilder">The health check builder instance.</param>
    /// <returns>The health check builder instance.</returns>
    public static IHealthChecksBuilder AddEnrollmentsApiHealthCheck(this IHealthChecksBuilder healthChecksBuilder)
    {
        return healthChecksBuilder.Add(
            new HealthCheckRegistration(
                "uri-enrollments-api",
                sp =>
                {
                    var clientConfig = sp.GetRequiredService<IOptions<EnrollmentApiHttpClientConfig>>().Value;
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
