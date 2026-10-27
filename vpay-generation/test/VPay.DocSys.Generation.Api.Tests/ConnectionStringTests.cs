using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using VPay.DocSys.Common;
using VPay.DocSys.Data;
using VPay.DocSys.Storage;
using Xunit;

namespace VPay.DocSys.Generation.Api.Tests
{
    public class ConnectionStringTests(ITestOutputHelper output)
    {
        [Fact]
        public void ShouldParseDocSysConnectionString()
        {
            var settings = GetConfigurationBuilder()
                    .Build()
                    .GetSection("DocSysConnection")
                    .Get<DocSysConnectionSettings>();

            WriteConnectionString(settings.GetConnectionString());
        }

        [Fact]
        public void ShouldParseOptumIntegrationConnectionString()
        {
            var settings = GetConfigurationBuilder()
                    .Build()
                    .GetSection("OptumIntegrationConnection")
                    .Get<OptumIntegrationConnectionSettings>();

            WriteConnectionString(settings.GetConnectionString());
        }

        [Fact]
        public void ShouldParseStorageDocumentConnectionString()
        {
            var settings = GetConfigurationBuilder()
                    .Build()
                    .GetSection("StorageSettings:DocumentDatabase")
                    .Get<DbSettings>();

            WriteConnectionString(settings.GetConnectionString());
        }

        [Fact]
        public void ShouldParseStorageStateManagementConnectionString()
        {
            var settings = GetConfigurationBuilder()
                    .Build()
                    .GetSection("StorageSettings:StateManagementDatabase")
                    .Get<DbSettings>();

            WriteConnectionString(settings.GetConnectionString());
        }

        private static IConfigurationBuilder GetConfigurationBuilder() => new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

        private void WriteConnectionString(string connectionString)
        {
            output.WriteLine(connectionString);
            Assert.False(string.IsNullOrWhiteSpace(connectionString));
            var builder = new SqlConnectionStringBuilder(connectionString);
            // TODO - This is a temporary fix when migrating Microsoft.Data.SqlClient from 2.* to 7.*.
            // Once the proper certificates in place, we should remove this check and ensure that the connection string is encrypted.
            Assert.False(builder.Encrypt);
        }
    }
}
