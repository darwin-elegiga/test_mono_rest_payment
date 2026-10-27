using System.Diagnostics.CodeAnalysis;
using VPay.Extensions.Logging.GrayLog;

namespace VPay.Optum.PaymentRouting.RemittanceAdviceFile.Configurations
{
    [ExcludeFromCodeCoverage]
    public static class HostBuilderExtensions
    {
        public static IHostBuilder UseLogging(this IHostBuilder hostBuilder) =>
            hostBuilder.ConfigureLogging(logging => logging
                .ClearProviders()
                .AddConsole()
                .AddDebug()
                .AddVPayGrayLog());

        public static IHostBuilder BuildConfiguration(this IHostBuilder hostBuilder)
        {
            return hostBuilder.ConfigureAppConfiguration(configurationBuilder =>
            {
                configurationBuilder
                    .AddJsonFile("appsettings.Local.json", true, true)
                    .AddJsonFile("appsettings.secure.json", true, true);
            });
        }
    }
}
