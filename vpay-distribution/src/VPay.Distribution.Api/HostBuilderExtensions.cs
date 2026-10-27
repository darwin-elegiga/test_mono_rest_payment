using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VPay.Extensions.Logging.GrayLog;


namespace VPay.Distribution.Api
{
    public static class HostBuilderExtensions
    {
        public static IHostBuilder UseLogging(this IHostBuilder hostBuilder) =>
            hostBuilder.ConfigureLogging(logging =>
            {
                logging
                    .ClearProviders()
                    .AddConsole()
                    .AddDebug()
                    .AddVPayGrayLog()
                ;
            });

        public static IHostBuilder BuildConfiguration(this IHostBuilder hostBuilder) =>
            hostBuilder.ConfigureAppConfiguration((configurationBuilder) =>
            {
                string environmentName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
                if (string.IsNullOrWhiteSpace(environmentName))
                {
                    environmentName = "Development";
                }
                configurationBuilder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true)
                    .AddJsonFile($"appsettings.Local.json", optional: true, reloadOnChange: true)
                    .AddJsonFile($"appsettings.secure.json", optional: true, reloadOnChange: true)
                    .AddJsonFile($"appsettings.about.json", optional: true, reloadOnChange: true);
            });
    }
}
