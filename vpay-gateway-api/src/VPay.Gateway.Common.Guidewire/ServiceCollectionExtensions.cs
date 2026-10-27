using Microsoft.Extensions.DependencyInjection;
using VPay.Gateway.Common.Guidewire.OptumAPI;

namespace VPay.Gateway.Common.Guidewire;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add the services related to Guidewire to the <paramref name="serviceCollection"/>.
    /// </summary>
    /// <param name="serviceCollection">The collection to add the services to</param>
    /// <returns>The <paramref name="serviceCollection"/>.</returns>
    public static IServiceCollection AddGuidewire(this IServiceCollection serviceCollection) =>
        serviceCollection
            .AddSingleton<IOptumAPIPortTypeClientPolicyRegistry, OptumAPIPortTypeClientPolicyRegistry>()
            .AddTransient<IOptumAPIPortTypeClientFactory, OptumAPIPortTypeClientFactory>()
            .AddTransient<IGuidewireServiceProxy, GuidewireServiceProxy>();
}
