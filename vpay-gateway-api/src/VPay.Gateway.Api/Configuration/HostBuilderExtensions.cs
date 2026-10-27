using VPay.Extensions.Logging.GrayLog;

namespace VPay.Gateway.Api.Configuration;

public static class HostBuilderExtensions
{
    public static IHostBuilder UseLogging(this IHostBuilder hostBuilder) =>
        hostBuilder.ConfigureLogging(logging =>
        {
            logging
                .ClearProviders()
                .AddConsole()
                .AddDebug()
                .AddVPayGrayLog();
        });

    public static IHostBuilder BuildConfiguration(this IHostBuilder hostBuilder) =>
        hostBuilder.ConfigureAppConfiguration(configurationBuilder =>
        {
            configurationBuilder
                .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
                .AddJsonFile("appsettings.secure.json", optional: true, reloadOnChange: true);
        });
}
