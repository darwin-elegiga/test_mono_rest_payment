using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Serialization;
using Polly;
using Polly.Registry;
using Refit;
using Refit.Insane.PowerPack.Caching;
using VPay.DocSys.Common;
using VPay.DocSys.Parsing.Http.Abstractions;
using VPay.DocSys.Parsing.Http.Abstractions.PlatformConfiguration;
using VPay.DocSys.Parsing.Http.Constants;
using VPay.Extensions.Http;

namespace VPay.DocSys.Parsing.Http.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Inject Polly retry policies, and Refit client interfaces, and cached wrapper interfaces
        /// </summary>
        /// <param name="services"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static IServiceCollection ConfigureRefitApiDependencies(
            this IServiceCollection services, ApiSettings settings)
        {
            services.AddPollyHttpRetryPolicies();

            if (settings.ContainsService(ServiceNames.PLATFORM_CONFIGURATION_API))
            {
                services
                    .AddRefitApiClient<IPlatformConfigurationApi>(
                        o => o.GetRequiredService<ApiSettings>()[ServiceNames.PLATFORM_CONFIGURATION_API]);
            }

            return services;
        }

        /// <summary>
        /// Adds Polly retry policies for http calls (waited and unwaited)
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
        /// Adds refit service for specified Api Interface and Options
        /// </summary>
        /// <typeparam name="TApiInterface"></typeparam>
        /// <typeparam name="THttpClientOptions"></typeparam>
        /// <param name="services"></param>
        /// <param name="clientOptions"></param>
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

            services.TryAddTransient<RemoveDoubleSlashDelegatingHandler>();

            return services
                .AddRefitClient<TApiInterface>(settings)
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
        /// Adds health check for specified Api, confirming accessibility
        /// </summary>
        /// <typeparam name="THttpClientOptions"></typeparam>
        /// <param name="healthChecksBuilder"></param>
        /// <param name="configurationKey">IConfiguration key to the ExternalHttpOptions section</param>
        /// <returns></returns>
        public static IHealthChecksBuilder AddRefitApiCheck<TApiInterface>(
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
