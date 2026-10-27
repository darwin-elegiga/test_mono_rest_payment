using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using System.Security.Cryptography;

namespace VPay.Optum.Http
{
    [ExcludeFromCodeCoverage]
    public static class PollyConfigurationExtensions
    {
        public static IServiceCollection AddPolicies(
            this IServiceCollection services,
            Action<PolicyOptions> configureOptions)
        {
            var policyOptions = new PolicyOptions();
            configureOptions(policyOptions);

            var policyRegistry = services.AddPolicyRegistry();
            policyRegistry.Add(
                PolicyNames.HttpRetry,
                HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .WaitAndRetryAsync(3, _ => TimeSpan.FromMilliseconds(200) + TimeSpan.FromMilliseconds(RandomNumberGenerator.GetInt32(0, 100))));

            // Add a no-op policy for use where we don't want to do retries or anything like that (eg. HTTP POSTs)
            policyRegistry.Add(
                PolicyNames.NoOp,
                Policy.NoOpAsync<HttpResponseMessage>());

            return services;
        }
    }
}
