using System.Net.Http.Headers;
using HealthChecks.Uris;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Refit;
using VPay.Gateway.Common.Http;

namespace VPay.Gateway.Common.PlatformConfig;

public static class PlatformConfigApiConfigurationExtensions
{
    private const string ExternalClientConfigUri = "api/external-client-configuration/can-access";

    /// <summary>
    /// Adds the Documents API client to the <paramref name="services"/> collection.
    /// </summary>
    /// <param name="services">The collection of services to add the client to.</param>
    /// <param name="configureHttpClient">Action to set up configuration values.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddPlatformConfigApiClient(this IServiceCollection services, Action<PlatformConfigApiHttpClientConfig> configureHttpClient)
    {
        var jsonSettings = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };
        /// Currently <see cref="IPlatformConfigApi.GetOptumPayer(string, int, int, CancellationToken)"/> returns an array
        /// but it should return a <see cref="Pagination{T}"/>. This converter will handle either one until it is changed.
        jsonSettings.Converters.Add(new PaginationOrArrayNewtonsoftJsonConverter<OptumSubPayerDto>());
        var settings = new RefitSettings(new NewtonsoftJsonContentSerializer(jsonSettings));

        // try to register the logging handler if it hasn't already been registered
        services.TryAddTransient<HttpLoggingHandler>();

        return services
            .Configure<PlatformConfigApiHttpClientConfig>(configureHttpClient)
            .AddTransient(cfg => cfg.GetService<IOptions<PlatformConfigApiHttpClientConfig>>()!.Value)
            .AddRefitClient<IPlatformConfigApi>(settings)
            .ConfigureHttpClient((sp, client) =>
            {
                var clientConfig = sp.GetRequiredService<IOptions<PlatformConfigApiHttpClientConfig>>().Value;

                client.BaseAddress = clientConfig.BaseAddress;
                client.Timeout = clientConfig.Timeout;
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", clientConfig.ApiToken);
            })
            .AddPolicyHandlerFromRegistry(PolicyNames.HttpRetry)
            .AddHttpMessageHandler<HttpLoggingHandler>()
            .Services;
    }

    /// <summary>
    /// Adds a health check for the Platform Config API.
    /// </summary>
    /// <param name="healthChecksBuilder">The health check builder instance.</param>
    /// <returns>The health check builder instance.</returns>
    public static IHealthChecksBuilder AddPlatformConfigHealthCheck(this IHealthChecksBuilder healthChecksBuilder)
    {
        return healthChecksBuilder.Add(
            new HealthCheckRegistration(
                "uri-platform-config-api",
                sp =>
                {
                    var clientConfig = sp.GetRequiredService<IOptions<PlatformConfigApiHttpClientConfig>>().Value;
                    var uri = new Uri(clientConfig.BaseAddress, new Uri("api/about", UriKind.Relative));
                    var options = new UriHealthCheckOptions().AddUri(uri, opt => opt.UseTimeout(clientConfig.Timeout));

                    var httpFactory = sp.GetRequiredService<IHttpClientFactory>();
                    return new UriHealthCheck(options, () => httpFactory.CreateClient());
                },
                default,
                default
            ));
    }

    /// <summary>
    /// Adds health check for the External Client Configuration endpoint in Platform Config API
    /// The endpoint is called by Gateway to obtain the Guidewire URL, user name, and password.
    /// The health check tests authenticated connectivity since the endpoint requires authentication
    /// using an API JWT Token Bearer stored in Vault. An unhealthy status means that the API Token
    /// stored in Vault for Platform Config API is invalid or has expired.
    /// </summary>
    /// <param name="healthChecksBuilder">The health check builder instance.</param>
    /// <returns>The health check builder instance.</returns>
    public static IHealthChecksBuilder AddExternalClientConfigHealthCheck(this IHealthChecksBuilder healthChecksBuilder)
    {
        return healthChecksBuilder.Add(
            new HealthCheckRegistration(
                "uri-external-client-config-api",
                sp =>
                {
                    var clientConfig = sp.GetRequiredService<IOptions<PlatformConfigApiHttpClientConfig>>().Value;
                    if (clientConfig.BaseAddress == null)
                    {
                        throw new InvalidOperationException("The URI Base Address is null. Ensure that the configuration is set correctly.");
                    }
                    var uri = new Uri(clientConfig.BaseAddress, new Uri(ExternalClientConfigUri, UriKind.Relative));
                    var options = new UriHealthCheckOptions().AddUri(uri, opt => opt.UseTimeout(clientConfig.Timeout));

                    var httpFactory = sp.GetRequiredService<IHttpClientFactory>();
                    var client = httpFactory.CreateClient();

                    if (string.IsNullOrWhiteSpace(clientConfig.ApiToken))
                    {
                        throw new InvalidOperationException("The API token is null or empty. Ensure that the configuration is set correctly.");
                    }
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", clientConfig.ApiToken);
                    return new UriHealthCheck(options, () => client);
                },
                default,
                default
            ));
    }
}
