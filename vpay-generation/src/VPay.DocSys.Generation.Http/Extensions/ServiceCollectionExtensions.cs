using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Serialization;
using Polly;
using Polly.Registry;
using Refit;
using VPay.DocSys.Generation.Http.Abstractions;
using VPay.DocSys.Generation.Http.Constants;
using VPay.DocSys.Common;
using VPay.Extensions.Http;

namespace VPay.DocSys.Generation.Http.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Inject Polly retry policies, and Refit client interfaces, and cached wrapper interfaces.
        /// </summary>
        /// <param name="services">The services collection.</param>
        /// <param name="configuration">The configuration properties.</param>
        /// <returns></returns>
        public static IServiceCollection ConfigureRefitHttpClients(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var settings = configuration.GetSection("ApiSettings").Get<ApiSettings>();
            if (settings == null)
            {
                return services;
            }

            services.Configure<ApiSettings>(configuration.GetSection("ApiSettings"));
            services.Configure<ApiRetrySettings>(configuration.GetSection("ApiRetrySettings"));
            services.AddPollyHttpRetryPolicies();

            if (settings.ContainsService(ServiceNames.REMIT_PARTNER_NETWORK_API))
            {
                var apiToken = configuration.GetSection("RemittancePartnerNetworkApiRequestHeaders").GetValue<string>("ApiToken");
                services
                    .AddRefitApiClientWithAuth<IRemitPartnerNetworkApi>(
                        o => o.GetRequiredService<IOptionsMonitor<ApiSettings>>().CurrentValue[ServiceNames.REMIT_PARTNER_NETWORK_API], apiToken);
            }

            if (settings.ContainsService(ServiceNames.PLATFORM_CONFIGURATION_API))
            {
                services
                    .AddRefitApiClient<IPlatformConfigurationApi>(
                        o => o.GetRequiredService<IOptionsMonitor<ApiSettings>>().CurrentValue[ServiceNames.PLATFORM_CONFIGURATION_API]);
            }

            if (settings.ContainsService(ServiceNames.PAYMENT_ROUTING_API))
            {
                services
                    .AddRefitApiClient<IPaymentRoutingApi>(
                        o => o.GetRequiredService<IOptionsMonitor<ApiSettings>>().CurrentValue[ServiceNames.PAYMENT_ROUTING_API]);
            }

            return services;
        }

        /// <summary>
        /// Adds Polly retry policies for http calls (waited and not waited).
        /// </summary>
        /// <remarks><see cref="IOptions{THttpOptions}"/> must be registered</remarks>
        /// <param name="services"></param>
        /// <param name="retryOptions"></param>
        /// <returns></returns>
        public static IServiceCollection AddPollyHttpRetryPolicies(
            this IServiceCollection services)
            => services.AddSingleton<IReadOnlyPolicyRegistry<string>, PolicyRegistry>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<ApiRetrySettings>>();

                var registry = sp.GetService<IPolicyRegistry<string>>()
                    ?? new PolicyRegistry();

                // Add a retry policy for certain http requests
                if (!registry.ContainsKey(PolicyNames.HTTP_RETRY))
                {
                    registry.Add(
                        PolicyNames.HTTP_RETRY,
                        Polly.Extensions.Http.HttpPolicyExtensions
                            .HandleTransientHttpError()
                            .WaitAndRetryAsync(
                                options.Value.Count,
                                _ => TimeSpan.FromMilliseconds(RandomNumberGenerator.GetInt32(
                                    options.Value.MinWaitMs, options.Value.MaxWaitMs))));
                }

                // Add a no-op policy for use where we don't want to do retries or anything like that (eg. HTTP POSTs)
                if (!registry.ContainsKey(PolicyNames.NO_OP))
                {
                    registry.Add(
                        PolicyNames.NO_OP,
                        Policy.NoOpAsync<HttpResponseMessage>());
                }

                return (PolicyRegistry)registry;
            });

        /// <summary>
        /// Adds Refit service with OAuth authentication for specified API Interface, Refit Settings and Options.
        /// </summary>
        /// <typeparam name="TApiInterface">The API client interface.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="getServiceSettings">The function to get service settings.</param>
        /// <param name="apiToken">The API token.</param>
        /// <returns></returns>
        public static IHttpClientBuilder AddRefitApiClientWithAuth<TApiInterface>(
            this IServiceCollection services,
            Func<IServiceProvider, ServiceSettings> getServiceSettings,
            string? apiToken)
            where TApiInterface : class
        {
            var settings = new RefitSettings(
                new NewtonsoftJsonContentSerializer(
                    new Newtonsoft.Json.JsonSerializerSettings
                    {
                        ContractResolver = new CamelCasePropertyNamesContractResolver()
                    }));

            if (!string.IsNullOrWhiteSpace(apiToken))
            {
                settings.AuthorizationHeaderValueGetter = (_, cancellationToken) => Task.FromResult($"Bearer {apiToken}");
            }
            return AddRefitClient<TApiInterface>(services, getServiceSettings, settings);
        }

        /// <summary>
        /// Adds Refit service for specified API Interface, Refit Settings and Options.
        /// </summary>
        /// <typeparam name="TApiInterface">The API client interface.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="getServiceSettings">The function to get service settings.</param>
        /// <returns></returns>
        public static IHttpClientBuilder AddRefitApiClient<TApiInterface>(
            this IServiceCollection services,
            Func<IServiceProvider, ServiceSettings> getServiceSettings)
            where TApiInterface : class
        {
            var settings = new RefitSettings(
                new NewtonsoftJsonContentSerializer(
                    new Newtonsoft.Json.JsonSerializerSettings
                    {
                        ContractResolver = new CamelCasePropertyNamesContractResolver()
                    }));

            return AddRefitClient<TApiInterface>(services, getServiceSettings, settings);
        }

        /// <summary>
        /// Adds Refit service for specified API Interface and Options.
        /// </summary>
        /// <typeparam name="TApiInterface">The API client interface.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="getServiceSettings">The function to get service settings.</param>
        /// <param name="refitSettings">The Refit settings.</param>
        /// <returns></returns>
        private static IHttpClientBuilder AddRefitClient<TApiInterface>(
            IServiceCollection services,
            Func<IServiceProvider, ServiceSettings> getServiceSettings,
            RefitSettings refitSettings) where TApiInterface : class
        {
            services.TryAddTransient<RemoveDoubleSlashDelegatingHandler>();

            return services
                .AddRefitClient<TApiInterface>(refitSettings)
                .ConfigureHttpClient((sp, http) =>
                {
                    var options = getServiceSettings(sp);
                    http.BaseAddress = options.GetUri();
                    http.Timeout = options.Timeout;
                })
                .AddPolicyHandlerFromRegistry((policyRegistry, message) =>
                {
                    var name = message.Method == HttpMethod.Get || message.Method == HttpMethod.Head ? PolicyNames.HTTP_RETRY : PolicyNames.NO_OP;
                    return policyRegistry.Get<IAsyncPolicy<HttpResponseMessage>>(name);
                })
                .AddHttpMessageHandler<RemoveDoubleSlashDelegatingHandler>();
        }

        /// <summary>
        /// Adds health check for specified API, confirming accessibility.
        /// </summary>
        /// <typeparam name="TApiInterface">The API client interface.</typeparam>
        /// <param name="healthChecksBuilder">The health checks builder.</param>
        /// <param name="name">The health check name.</param>
        /// <param name="ensureHealth">The function to ensure health.</param>
        /// <returns></returns>
        public static IHealthChecksBuilder AddRefitApiHealthCheck<TApiInterface>(
            this IHealthChecksBuilder healthChecksBuilder,
            string name,
            Func<TApiInterface, Task<HealthStatus>> ensureHealth)
            where TApiInterface : class
        {
            return healthChecksBuilder.Add(
                new HealthCheckRegistration(
                    name,
                    sp => new RefitApiHealthCheck<TApiInterface>(sp.GetRequiredService<TApiInterface>(), ensureHealth),
                    default,
                    default
                ));
        }
    }
}
