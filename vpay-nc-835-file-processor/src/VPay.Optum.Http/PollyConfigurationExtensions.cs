using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

namespace VPay.Optum.Http
{
    public static class PollyConfigurationExtensions
    {
        public static IServiceCollection AddPolicies(this IServiceCollection services)
        {
            var jitterer = new Random();
            var policyRegistry = services.AddPolicyRegistry();
            policyRegistry.Add(
                PolicyNames.HttpRetry,
                HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .WaitAndRetryAsync(3, _ => TimeSpan.FromMilliseconds(200) + TimeSpan.FromMilliseconds(jitterer.Next(0, 100))));

            // Add a no-op policy for use where we don't want to do retries or anything like that (eg. HTTP POSTs)
            policyRegistry.Add(
                PolicyNames.NoOp,
                Policy.NoOpAsync<HttpResponseMessage>());

            return services;
        }
    }
}
