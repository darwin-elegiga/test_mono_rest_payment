using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using VPay.Data.Db2.Odbc;
using VPay.Data.Db2.Odbc.Helpers;
using VPay.DocSys.Common;
using VPay.DocSys.Storage;
using VPay.DocSys.Data;
using System.Data.Odbc;
using VPay.DocSys.Parsing.Http.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using VPay.DocSys.Parsing.Http.Abstractions.PlatformConfiguration;

namespace VPay.DocSys.Parsing.Api
{
    public static class ConfigureHealthChecks
    {
        public static IServiceCollection AddParsingHealthChecks(this IServiceCollection services, IConfiguration configuration)
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

                    // add file exists check for EobMergeSettings
                    .AddFileExistsCheck(name: "EobMergeSettings",
                        filePathFactory: (provider) => provider.GetRequiredService<IConfiguration>().GetSection("StatusFile").GetValue<string>("EobMergeSettings"))

                    // add file exists check for EobMergeConfig
                    .AddFileExistsCheck(name: "EobMergeConfig",
                        filePathFactory: (provider) => provider.GetRequiredService<IConfiguration>().GetSection("StatusFile").GetValue<string>("EobMergeConfig"))

                    // add folder exists check for PythonHome
                    .AddFolderExistsCheck(name: "PythonHome",
                        folderPathFactory: (provider) => provider.GetRequiredService<IConfiguration>().GetSection("StatusFile").GetValue<string>("PythonHome"))

                    // add folder exists check for FileHome
                    .AddFolderExistsCheck(name: "FileHome",
                        folderPathFactory: (provider) => provider.GetRequiredService<IConfiguration>().GetSection("StatusFile").GetValue<string>("FileHome"))

                    // add folder exists check for TarFolder
                    .AddFolderExistsCheck(name: "TarFolder",
                        folderPathFactory: (provider) => provider.GetRequiredService<IConfiguration>().GetSection("StatusFile").GetValue<string>("FileHome") + provider.GetRequiredService<IConfiguration>().GetSection("StatusFile").GetValue<string>("TarFolder"))

                    // add DocSysDB check
                    .AddSqlServerCheck(name: "DocSys Database",
                        connectionStringFactory: provider => provider.GetService<DocSysConnectionSettings>().GetConnectionString(),
                        connectTimeout: healthCheckSettings.SqlServerConnectTimeout,
                        commandTimeout: healthCheckSettings.SqlServerCommandTimeout)

                    // add storage manager check
                    .AddStorageManagerChecks(name: "StorageManager",
                        documentDbConnectTimeout: healthCheckSettings.SqlServerConnectTimeout,
                        documentDbCommandTimeout: healthCheckSettings.SqlServerCommandTimeout,
                        stateManagementDbConnectTimeout: healthCheckSettings.SqlServerConnectTimeout,
                        stateManagementDbCommandTimeout: healthCheckSettings.SqlServerCommandTimeout)

                    // add db2 check
                    .AddOdbcCheck(name: $"Db2 Database",
                        connectionStringFactory: provider => provider.GetService<OdbcConnectionConfig>().GetConnectionString(),
                        connectTimeout: healthCheckSettings.Db2ConnectTimeout,
                        commandTimeout: healthCheckSettings.Db2CommandTimeout)

                    .AddHttpEndpointCheck(name: "Distribution API",
                        serviceSettingsFactory: provider => provider.GetRequiredService<ApiSettings>()["DistributionApi"],
                        endPoint: "/api/about")

                    // add custom check using Refit for PLATFORM_CONFIGURATION_API
                    .AddRefitApiCheck<IPlatformConfigurationApi>(name: "PlatformConfigurationApiViaRefit",
                        ensureHealth: async api => { await api.GetAbout(); return HealthStatus.Healthy; })

                    // add rabbit mq check for StatusFileParsingQueue
                    .AddRabbitMQCheck(name: "StatusFileParsingQueue",
                        queueSettingsFactory: provider => provider.GetRequiredService<StatusFileParsingQueueSettings>(),
                        connectTimeout: healthCheckSettings.RabbitMQTimeout)

                    // add rabbit mq check for OutboundRedCardDataDriverFileParsingQueue
                    .AddRabbitMQCheck(name: "OutboundRedCardDataDriverFileParsingQueue",
                        queueSettingsFactory: provider => provider.GetRequiredService<OutboundRedCardDataQueueSettings>(),
                        connectTimeout: healthCheckSettings.RabbitMQTimeout)

                    // add rabbit mq check for InventoryDriverFileParsingQueue
                    .AddRabbitMQCheck(name: "InventoryDriverFileParsingQueue",
                        queueSettingsFactory: provider => provider.GetRequiredService<InventoryDriverFileQueueSettings>(),
                        connectTimeout: healthCheckSettings.RabbitMQTimeout)

                    // add rabbit mq check for DocumentTransactionQueue
                    .AddRabbitMQCheck(name: "DocumentTransactionQueue",
                        queueSettingsFactory: provider => provider.GetRequiredService<DocumentTransactionQueueSettings>(),
                        connectTimeout: healthCheckSettings.RabbitMQTimeout)

                    // add rabbit mq check for DocumentDistributionQueue
                    .AddRabbitMQCheck(name: "DocumentDistributionQueue",
                        queueSettingsFactory: provider => provider.GetRequiredService<DocumentDistributionQueueSettings>(),
                        connectTimeout: healthCheckSettings.RabbitMQTimeout)

                    // add rabbit mq check for RedCardDistributionQueue
                    .AddRabbitMQCheck(name: "RedCardDistributionQueue",
                        queueSettingsFactory: provider => provider.GetRequiredService<RedCardDistributionQueueSettings>(),
                        connectTimeout: healthCheckSettings.RabbitMQTimeout)

                    // add rabbit mq check for Inbound835FileQueue
                    .AddRabbitMQCheck(name: "Inbound835FileQueue",
                        queueSettingsFactory: provider => provider.GetRequiredService<Inbound835FileQueueSettings>(),
                        connectTimeout: healthCheckSettings.RabbitMQTimeout)

                    // add rabbit mq check for iSeriesPmbQueue
                    .AddRabbitMQCheck(name: "iSeriesPmbQueue",
                        queueSettingsFactory: provider => provider.GetRequiredService<iSeriesPmbQueueSettings>(),
                        connectTimeout: healthCheckSettings.RabbitMQTimeout)

                    // add rabbit mq check for Edi835GenerationQueue
                    .AddRabbitMQCheck(name: "Edi835GenerationQueue",
                        queueSettingsFactory: provider => provider.GetRequiredService<Edi835GenerationQueueSettings>(),
                        connectTimeout: healthCheckSettings.RabbitMQTimeout)

                // initialize readiness checks
                .AddReadinessChecks()

                    // with just the default check
                    .WithDefaultCheck();


            return services;
        }


    }
}
