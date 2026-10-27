using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using VPay.Distribution.Common;
using VPay.Distribution.Common.Models;
using VPay.DocSys.Common;
using VPay.DocSys.Data;
using VPay.DocSys.Storage;

namespace VPay.Distribution.Api
{
    public static class ConfigureHealthChecks
    {
        public static IServiceCollection AddDistributionHealthChecks(this IServiceCollection services, IConfiguration configuration)
        {
            // get health check settings
            var healthCheckSettings = configuration.GetSection("HealthCheckSettings").Get<HealthCheckSettings>();

            // initialize .net health checks framework
            services.AddDocSysHealthChecks()

                // initialize liveness checks 
                .AddLivenessChecks()

                    // with just the default check
                    .WithDefaultCheck()


                // initialize startup-only checks
                .AddStartupOnlyChecks()

                    // add folder exists checks
                    .AddFolderExistsCheck(name: "incronCtrl Folder",
                        folderPathFactory: (provider) =>
                    provider.GetRequiredService<AppSettings>().ControlPath)

                    // add folder exists checks
                    .AddFolderExistsCheck(name: "FaxBase Folder",
                        folderPathFactory: (provider) =>
                    provider.GetRequiredService<AppSettings>().FaxBase)

                    // add folder exists checks
                    .AddFolderExistsCheck(name: "CustomSendBack Folder",
                        folderPathFactory: (provider) =>
                    provider.GetRequiredService<AppSettings>().CustomSendBackPath)

                    // add docsys database check
                    .AddSqlServerCheck(name: "DocSys Database",
                        connectionStringFactory: provider => provider.GetService<DocSysConnectionSettings>().GetConnectionString(),
                        connectTimeout: healthCheckSettings.SqlServerConnectTimeout,
                        commandTimeout: healthCheckSettings.SqlServerCommandTimeout)

                // add rabbit mq check for TiffQueue
                .AddRabbitMQCheck(name: "Tiff Queue",
                    queueSettingsFactory: provider => provider.GetRequiredService<TiffQueueConfigurationSettings>(),
                    connectTimeout: healthCheckSettings.RabbitMQTimeout)

                // add rabbit mq check for DistributionQueue
                .AddRabbitMQCheck(name: "Distribution Queue",
                    queueSettingsFactory: provider => provider.GetRequiredService<DistributionQueueConfigurationSettings>(),
                    connectTimeout: healthCheckSettings.RabbitMQTimeout)

                // add rabbit mq check for RedCardDistributionQueue
                .AddRabbitMQCheck(name: "RedCard Distribution Queue",
                    queueSettingsFactory: provider => provider.GetRequiredService<RedCardDistributionQueueConfigurationSettings>(),
                    connectTimeout: healthCheckSettings.RabbitMQTimeout)

                // add rabbit mq check for FaxDistributionQueue
                .AddRabbitMQCheck(name: "Fax Distribution Queue",
                    queueSettingsFactory: provider => provider.GetRequiredService<FaxDistributionQueueConfigurationSettings>(),
                    connectTimeout: healthCheckSettings.RabbitMQTimeout)

                // add rabbit mq check for MailDistributionQueue
                .AddRabbitMQCheck(name: "Mail Distribution Queue",
                    queueSettingsFactory: provider => provider.GetRequiredService<MailDistributionQueueConfigurationSettings>(),
                    connectTimeout: healthCheckSettings.RabbitMQTimeout)

                // add rabbit mq check for SendBackDistributionQueue
                .AddRabbitMQCheck(name: "SendBack Distribution Queue",
                    queueSettingsFactory: provider => provider.GetRequiredService<SendBackDistributionQueueConfigurationSettings>(),
                    connectTimeout: healthCheckSettings.RabbitMQTimeout)

                // add rabbit mq check for EventDistributionQueue
                .AddRabbitMQCheck(name: "Event Distribution Queue",
                    queueSettingsFactory: provider => provider.GetRequiredService<EventDistributionQueueConfigurationSettings>(),
                    connectTimeout: healthCheckSettings.RabbitMQTimeout)

                // add rabbit mq check for Edi835GenerationQueue
                .AddRabbitMQCheck(name: "Edi835 Generation Queue",
                    queueSettingsFactory: provider => provider.GetRequiredService<Edi835GenerationQueueConfigurationSettings>(),
                    connectTimeout: healthCheckSettings.RabbitMQTimeout)

                // add rabbit mq check for STPDistributionQueue
                .AddRabbitMQCheck(name: "STP Distribution Queue",
                    queueSettingsFactory: provider => provider.GetRequiredService<STPQueueConfigurationSettings>(),
                    connectTimeout: healthCheckSettings.RabbitMQTimeout)

                // initialize readiness checks
                .AddReadinessChecks()

                    // with just the default check
                    .WithDefaultCheck();

            return services;
        }

    }
}
