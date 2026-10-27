using Microsoft.Extensions.Options;
using Newtonsoft.Json.Serialization;
using Polly;
using Refit;
using VPay.Gateway.Common;
using VPay.Gateway.Common.PepClaims;

namespace VPay.Gateway.ClientProcessor.Configurations;

public static class PepClaimsApiConfigurationExtensions
{
    private const string PepClaimsApiHttpClientConfigSectionName = "PepClaimsApiHttpClientConfig";

    /// <summary>
    /// Adds the Recon API client to the <paramref name="services"/> collection.
    /// </summary>
    /// <param name="services">The collection of services to add the client to.</param>
    /// <param name="configuration">Configuration properties.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddPepClaimsApiClient(this IServiceCollection services, IConfiguration configuration)
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

        return services
            .Configure<PepClaimsApiHttpClientConfig>(configuration.GetSection(PepClaimsApiHttpClientConfigSectionName))
            .AddTransient(cfg => cfg.GetService<IOptions<PepClaimsApiHttpClientConfig>>().Value)
            .AddRefitClient<IPepClaimsApi>(settings)
            .ConfigureHttpClient((sp, client) =>
            {
                var clientConfig = sp.GetRequiredService<IOptions<PepClaimsApiHttpClientConfig>>().Value;

                client.BaseAddress = clientConfig.BaseAddress;
                client.Timeout = clientConfig.Timeout;
            })
            .AddPolicyHandlerFromRegistry((policyRegistry, message) =>
                policyRegistry.Get<IAsyncPolicy<HttpResponseMessage>>(message.Method == HttpMethod.Get || message.Method == HttpMethod.Head ? PolicyNames.HttpRetry : PolicyNames.NoOp))
            .Services;
    }
}
