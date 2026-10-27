using Microsoft.Extensions.Options;

namespace VPay.Gateway.Management.Api.Configuration;

public static class ServiceCollectionExtensions
{
    internal record ServiceConfiguration(IServiceCollection Services, IConfiguration Configuration);

    internal static ServiceConfiguration ForConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        return new ServiceConfiguration(services, configuration);
    }

    internal static ServiceConfiguration AddSettings<TSettingsModel>(this ServiceConfiguration sc, string sectionName = null)
        where TSettingsModel : class
    {
        IConfigurationSection section = sc.Configuration.GetSection(sectionName ?? typeof(TSettingsModel).Name);
        sc.Services.Configure<TSettingsModel>(section);
        sc.Services.AddTransient(cfg => cfg.GetRequiredService<IOptions<TSettingsModel>>().Value);

        return sc;
    }
}
