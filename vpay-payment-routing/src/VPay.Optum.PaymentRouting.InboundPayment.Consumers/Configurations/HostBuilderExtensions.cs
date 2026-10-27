using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Gelf.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VPay.Extensions.Exceptions;
using VPay.Extensions.Logging.GrayLog;

namespace VPay.Optum.PaymentRouting.InboundPayment.Consumers.Configurations
{
    [ExcludeFromCodeCoverage]
    public static class HostBuilderExtensions
    {
        public static IHostBuilder UseLogging(this IHostBuilder hostBuilder) =>
            hostBuilder.ConfigureLogging(logging => logging
                    .ClearProviders()
                    .AddConsole()
                    .AddDebug()
                    .AddVPayGrayLog((GelfLoggerOptions options) =>
                        options.AdditionalFieldsFactory = (logLevel, eventId, exception) =>
                        {
                            if (exception is IAlertableException e)
                            {
                                var ls = new Dictionary<string, object>();

                                ls[nameof(e.AlertNotification)] = e.AlertNotification;
                                ls[nameof(e.AlertMessageTitle)] = e.AlertMessageTitle;
                                ls[nameof(e.AlertMessageSummary)] = e.AlertMessageSummary;
                                ls[nameof(e.AlertMessageDetails)] = e.AlertMessageDetails;
                                return ls;
                            }
                            return null;
                        }
                ));

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
