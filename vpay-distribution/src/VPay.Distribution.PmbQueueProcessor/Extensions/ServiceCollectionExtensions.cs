using com.ibm.as400.access;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VPay.Distribution.PmbQueueProcessor.As400Integration;
using VPay.Distribution.PmbQueueProcessor.As400Integration.Interfaces;
using VPay.Distribution.PmbQueueProcessor.As400Integration.Wrappers;
using VPay.Distribution.PmbQueueProcessor.Services;
using VPay.Distribution.PmbQueueProcessor.Settings;

namespace VPay.Distribution.PmbQueueProcessor.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAs400DataQueueService(this IServiceCollection services, IConfiguration configuration)
    {
        var as400SettingsSection = configuration.GetSection("As400Settings");
        var as400Settings = as400SettingsSection.Get<As400Settings>();
        services.AddSingleton(as400Settings);

        services.AddSingleton<AS400ConnectionPool>();
        services.AddSingleton<IAs400ConnectionPool, As400ConnectionPoolWrapper>();
        services.AddSingleton<IAs400ObjectFactory, As400ObjectFactory>();
        services.AddScoped<IAs400DataQueueService, As400DataQueueService>();
        return services;
    }
}
