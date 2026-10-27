using HealthChecks.Uris;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Refit;
using VPay.Gateway.Common.Http;

namespace VPay.Gateway.Common.MemberClaims;

public static class MemberClaimsApiConfigurationExtensions
{
    /// <summary>
    /// Adds the Member Claims API client to the <paramref name="services"/> collection.
    /// </summary>
    /// <param name="services">The collection of services to add the client to.</param>
    /// <param name="configureHttpClient">Action to set up configuration values.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddMemberClaimsApiClient(this IServiceCollection services, Action<MemberClaimsApiHttpClientConfig> configureHttpClient)
    {
        var settings = new RefitSettings(new NewtonsoftJsonContentSerializer(new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() }));

        // try to register the logging handler if it hasn't already been registered
        services.TryAddTransient<HttpLoggingHandler>();

        return services
            .Configure(configureHttpClient)
            .AddTransient(cfg => cfg.GetRequiredService<IOptions<MemberClaimsApiHttpClientConfig>>().Value)
            .AddRefitClient<IMemberClaimsApi>(settings)
            .ConfigureHttpClient((sp, client) =>
            {
                var clientConfig = sp.GetRequiredService<IOptions<MemberClaimsApiHttpClientConfig>>().Value;

                client.BaseAddress = clientConfig.BaseAddress;
                client.Timeout = clientConfig.Timeout;
            })
            .AddHttpMessageHandler<HttpLoggingHandler>()
            .Services;
    }

    /// <summary>
    /// Adds a health check for the Member Claims API
    /// </summary>
    /// <param name="healthChecksBuilder">The health check builder instance.</param>
    /// <returns>The health check builder instance.</returns>
    public static IHealthChecksBuilder AddMemberClaimsHealthCheck(this IHealthChecksBuilder healthChecksBuilder)
    {
        return healthChecksBuilder.Add(
            new HealthCheckRegistration(
                "uri-member-claims-api",
                sp =>
                {
                    var clientConfig = sp.GetRequiredService<IOptions<MemberClaimsApiHttpClientConfig>>().Value;
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
