using System;
using System.Data.Odbc;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using VPay.Data.Db2.Odbc;
using VPay.Data.Db2.Odbc.Helpers;
using VPay.DocSys.Common;
using VPay.DocSys.Data;
using VPay.DocSys.Generation.Common.Edi835;
using VPay.DocSys.Generation.Configs;
using VPay.DocSys.Generation.Models;
using VPay.DocSys.Storage;

namespace VPay.DocSys.Generation.Api
{
    public static class ConfigureHealthChecks
    {
        public static IServiceCollection AddGenerationHealthChecks(this IServiceCollection services, IConfiguration configuration)
        {
            // get health check settings
            var healthCheckSettings = configuration.GetSection("HealthCheckSettings").Get<HealthCheckSettings>();

            // initialize .net health checks framework
            services
                .AddDocSysHealthChecks()

                // initialize liveness checks
                .AddLivenessChecks()

                // with just the default check
                .WithDefaultCheck()

                // initialize startup-only checks
                .AddStartupOnlyChecks()

                // add file exists check for DropToPrint python script
                .AddFileExistsCheck(name: "DropToPrint",
                    filePathFactory: p => p.GetRequiredService<DropToPrintSettings>().PythonScript)

                // add file exists check for CustomSendBackCheckMaker python script
                .AddFileExistsCheck(
                    name: "CustomSendBackCheckMaker",
                    filePathFactory: p => p.GetRequiredService<CustomSendBackOperations>().CheckMaker)

                // add file exists check for whitebox.pdf
                .AddFileExistsCheck(name: "WhiteBox.pdf",
                    filePathFactory: p => p.GetRequiredService<DocSysSettings>().WhiteBoxPdf)

                // add DocSysDB check
                .AddSqlServerCheck(name: "DocSys Database",
                    connectionStringFactory: provider =>
                        provider.GetService<DocSysConnectionSettings>().GetConnectionString(),
                    connectTimeout: healthCheckSettings.SqlServerConnectTimeout,
                    commandTimeout: healthCheckSettings.SqlServerCommandTimeout)

                // add OptumIntegrationDB check
                .AddSqlServerCheck(name: "OptumIntegration Database",
                    connectionStringFactory: provider =>
                        provider.GetService<OptumIntegrationConnectionSettings>().GetConnectionString(),
                    connectTimeout: healthCheckSettings.SqlServerConnectTimeout,
                    commandTimeout: healthCheckSettings.SqlServerCommandTimeout)

                // add storage manager check
                .AddStorageManagerChecks(name: "StorageManager",
                    documentDbConnectTimeout: healthCheckSettings.MySqlConnectTimeout,
                    documentDbCommandTimeout: healthCheckSettings.MySqlCommandTimeout,
                    stateManagementDbConnectTimeout: healthCheckSettings.SqlServerConnectTimeout,
                    stateManagementDbCommandTimeout: healthCheckSettings.SqlServerCommandTimeout)

                //// add db2 check
                .AddOdbcCheck(name: $"Db2 Database",
                   connectionStringFactory: provider =>
                       provider.GetService<OdbcConnectionConfig>().GetConnectionString(),
                   connectTimeout: healthCheckSettings.Db2ConnectTimeout,
                   commandTimeout: healthCheckSettings.Db2CommandTimeout)

                // add http endpoint check for FOP
                .AddHttpEndpointCheck(name: "FOP",
                    serviceSettingsFactory: p => p.GetService<ApiSettings>()["FOP"],
                    endPoint: "",
                    timeout: healthCheckSettings.ApiServiceTimeout,
                    headers: new (string, string)[] { ("Accept", "application/json") })

                // add http endpoint check for ClientAssetServer
                .AddHttpEndpointCheck(name: "ClientAssetServer",
                    serviceSettingsFactory: p => p.GetService<ApiSettings>()["StaticFileApi"],
                    endPoint: "",
                    timeout: healthCheckSettings.ApiServiceTimeout,
                    headers: new (string, string)[] { ("Accept", "application/json") })

                // add http endpoint check for PaymentRoutingApi
                .AddHttpEndpointCheck(name: "PaymentRoutingApi",
                    serviceSettingsFactory: p => p.GetService<ApiSettings>()["PaymentRoutingApi"],
                    endPoint: "api/about",
                    timeout: healthCheckSettings.ApiServiceTimeout,
                    headers: new (string, string)[] { ("Accept", "application/json") })

                // add http endpoint check for PlatformConfigurationApi
                .AddHttpEndpointCheck(name: "PlatformConfigurationApi",
                    serviceSettingsFactory: p => p.GetService<ApiSettings>()["PlatformConfigurationApi"],
                    endPoint: "api/about",
                    timeout: healthCheckSettings.ApiServiceTimeout,
                    headers: new (string, string)[] { ("Accept", "application/json") })

                // add http endpoint check for GreenArrowApi
                .AddHttpEndpointCheck(name: "GreenArrowApi",
                    serviceSettingsFactory: p => p.GetService<ApiSettings>()["GreenArrowApi"],
                    endPoint: "",
                    timeout: healthCheckSettings.ApiServiceTimeout,
                    headers: new (string, string)[] { ("Accept", "application/json") })

                // add http endpoint check for ParsingApi
                .AddHttpEndpointCheck(name: "ParsingApi",
                    serviceSettingsFactory: p => p.GetService<ApiSettings>()["ParsingApi"],
                    endPoint: "api/about",
                    timeout: healthCheckSettings.ApiServiceTimeout,
                    headers: new (string, string)[] { ("Accept", "application/json") })

                // add rabbit mq check for DocumentGeneration
                .AddRabbitMQCheck(name: "DocumentGeneration",
                    queueSettingsFactory: p => p.GetRequiredService<QueueConfigurations>().DocumentGenerationQueue,
                    connectTimeout: healthCheckSettings.RabbitMQTimeout)

                // add rabbit mq check for CorrespondenceQueue
                .AddRabbitMQCheck(name: "CorrespondenceQueue",
                    queueSettingsFactory: p => p.GetRequiredService<QueueConfigurations>().CorrespondenceQueue,
                    connectTimeout: healthCheckSettings.RabbitMQTimeout)

                // add rabbit mq check for BundledFax
                .AddRabbitMQCheck(name: "BundledFaxQueue",
                    queueSettingsFactory: p => p.GetRequiredService<QueueConfigurations>().BundledFaxQueue,
                    connectTimeout: healthCheckSettings.RabbitMQTimeout)

                // add rabbit mq check for FaxQueue
                .AddRabbitMQCheck(name: "FaxMessageQueue",
                    queueSettingsFactory: p => p.GetRequiredService<QueueConfigurations>().FaxMessageQueue,
                    connectTimeout: healthCheckSettings.RabbitMQTimeout)

                // add rabbit mq check for TiffQueue
                .AddRabbitMQCheck(name: "TiffQueue",
                    queueSettingsFactory: p => p.GetRequiredService<QueueConfigurations>().TiffQueue,
                    connectTimeout: healthCheckSettings.RabbitMQTimeout)

                // add rabbit mq check for FaxJobDataQueue
                .AddRabbitMQCheck(name: "FaxJobDataQueue",
                    queueSettingsFactory: p => p.GetRequiredService<QueueConfigurations>().FaxJobDataQueue,
                    connectTimeout: healthCheckSettings.RabbitMQTimeout)

                // add rabbit mq check for FaxRetryQueue
                .AddRabbitMQCheck(name: "FaxRetryQueue",
                    queueSettingsFactory: p => p.GetRequiredService<QueueConfigurations>().FaxRetryQueue,
                    connectTimeout: healthCheckSettings.RabbitMQTimeout)

                // add rabbit mq check for ReminderFaxQueue
                .AddRabbitMQCheck(name: "ReminderFaxQueue",
                    queueSettingsFactory: p => p.GetRequiredService<QueueConfigurations>().ReminderFaxQueue,
                    connectTimeout: healthCheckSettings.RabbitMQTimeout)

                // add rabbit mq check for InventoryDriverRecordQueue
                .AddRabbitMQCheck(name: "InventoryDriverRecordQueue",
                    queueSettingsFactory: p => p.GetRequiredService<QueueConfigurations>().InventoryDriverRecordQueue,
                    connectTimeout: healthCheckSettings.RabbitMQTimeout)

                // add rabbit mq check for Edi835GenerationQueue
                .AddRabbitMQCheck(name: "Edi835GenerationQueue",
                    queueSettingsFactory: p => p.GetRequiredService<QueueConfigurations>().Edi835GenerationQueue,
                    connectTimeout: healthCheckSettings.RabbitMQTimeout)

                .AddFolderExistsCheck(name: "DocsFolder",
                    folderPathFactory: p => p.GetRequiredService<Edi835ServiceSettings>().LegacyDocsFolder)

                // initialize readiness checks
                .AddReadinessChecks()

                // with just the default check
                .WithDefaultCheck();

            return services;
        }
    }
}
