using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VPay.Extensions.Logging.GrayLog;

namespace VPay.DocSys.Generation.Edi835.Worker
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
                    .AddVPayGrayLog();

            });

        public static IHostBuilder BuildConfiguration(this IHostBuilder hostBuilder) =>
            hostBuilder.ConfigureAppConfiguration((configurationBuilder) =>
            {
                configurationBuilder
                    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
                    .AddJsonFile("appsettings.secure.json", optional: true, reloadOnChange: true);
            });
    }
}
