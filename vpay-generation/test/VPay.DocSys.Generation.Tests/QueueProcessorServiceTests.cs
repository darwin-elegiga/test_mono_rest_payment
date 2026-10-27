using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using VPay.Data.Db2.Odbc;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Configs;
using VPay.DocSys.Generation.Models;
using VPay.DocSys.Generation.QueueProcessor.Consolidated;
using VPay.DocSys.Pdf.Tools;
using VPay.DocSys.Storage;
using Xunit;

namespace VPay.DocSys.Generation.Tests
{
    public class QueueProcessorServiceTests
    {
        private readonly IServiceProvider _serviceProvider;

        public QueueProcessorServiceTests()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "StorageSettings:RootFolder", "test" },
                    { "StorageSettings:DocumentDatabase:DbSystemType", "SqlServer" },
                    { "StorageSettings:DocumentDatabase:Host", "test" },
                    { "StorageSettings:StateManagementDatabase:DbSystemType", "SqlServer" },
                    { "StorageSettings:StateManagementDatabase:Host", "test" },
                    { "DocSysConnection:Host", "test" },
                    { "DocSysConnection:Database", "test" },
                    { "DocSysConnection:UserName", "test" },
                    { "DocSysConnection:Password", "test" },
                    { "DocSysConnection:ApplicationName", "test" },
                    { "Db2:Dsn", "test" },
                    { "Db2:Hostname", "test" },
                    { "Db2:UserName", "test" },
                    { "Db2:Password", "test" },
                    { "Db2:DefaultLibraries", "test" },
                    { "RemittancePartnerNetworkApiOAuthSettings:IdServerUrl", "test" },
                    { "RemittancePartnerNetworkApiOAuthSettings:ClientId", "test" },
                    { "RemittancePartnerNetworkApiOAuthSettings:ClientSecret", "test" },
                    { "RemittancePartnerNetworkApiOAuthSettings:Scope", "test" },
                    { "RemittancePartnerNetworkApiOAuthSettings:GrantType", "test" },
                    { "ApiSettings:ServiceSettings:0:ServiceName", "RemittancePartnerNetworkApi" },
                    { "ApiSettings:ServiceSettings:0:Host", "test.com" }
                }).Build();
            var collection = new ServiceCollection();
            _serviceProvider =
                VPay.DocSys.Generation.QueueProcessor.Consolidated.Startup.AddServices(collection, configuration)
                .BuildServiceProvider();
        }

        [Fact]
        public void ShouldInject()
        {
            // assert
            Assert.NotNull(_serviceProvider.GetRequiredService<IOptions<ApiSettings>>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IOptions<DocSysSettings>>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IOptions<OdbcConnectionConfig>>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IStorageSettings>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IOptions<CustomSendBackOperations>>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IOptions<ProcessorSettings>>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IOptionsSnapshot<FssiTracSettings>>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IOptions<QueueConfigurations>>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IFileSystem>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IFssiTracManager>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IQueueService>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IPdfTools>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IDocSysOperations>());
            Assert.NotNull(_serviceProvider.GetRequiredService<ICleanDocsOperations>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IEobXmlParser>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IStorageManager>());
            Assert.NotNull(_serviceProvider.GetRequiredService<CleanDocsService>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IPythonRunner>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IProcessRunner>());
            Assert.NotNull(_serviceProvider.GetRequiredService<ICustomSendBackDocumentGeneratorService>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IEobDocumentGeneratorService>());
            Assert.NotNull(_serviceProvider.GetRequiredService<ICoverVPaygeGeneratorService>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IPacketDocumentGeneratorService>());
            Assert.NotNull(_serviceProvider.GetRequiredService<ISendBackCheckXmlFileWriter>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IFaxArchiveMessageProcessor>());
        }
    }
}
