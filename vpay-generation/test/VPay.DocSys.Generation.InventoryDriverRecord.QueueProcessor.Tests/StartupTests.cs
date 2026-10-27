using System;
using System.IO.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Configs;
using VPay.DocSys.Generation.Models;
using VPay.DocSys.Pdf.Tools;
using VPay.DocSys.Storage;
using Xunit;


namespace VPay.DocSys.Generation.InventoryDriverRecord.QueueProcessor.Tests
{
    /// <summary>
    /// Confirm that all controllers will have their arguments injected correctly
    /// </summary>
    public class StartupTests
    {
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;

        public StartupTests()
        {
            // setup mocks

            // setup SUT
            _configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
                .Build();
            _serviceProvider = new ServiceCollection()
                .AddSingleton<IConfiguration>(_configuration)
                .AddLogging(b => b.AddProvider(NullLoggerProvider.Instance))
                .AddServices(_configuration)
                .AddSingleton<InventoryDriverRecordProcessorService>()
                .BuildServiceProvider();
        }

        [Fact]
        public void AddServices_ShouldInjectForReminderFaxProcessorService()
        {
            Assert.NotNull(_serviceProvider.GetRequiredService<ProcessorSettings>());
            Assert.NotNull(_serviceProvider.GetRequiredService<PythonSettings>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IStorageSettings>());
            Assert.NotNull(_serviceProvider.GetRequiredService<CustomSendBackOperations>());
            Assert.NotNull(_serviceProvider.GetRequiredService<QueueConfigurations>().InventoryDriverRecordQueue);
            Assert.NotNull(_serviceProvider.GetRequiredService<InventoryDriverRecordProcessorService>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IInventoryDriverRecordMessageProcessor>());

            Assert.NotNull(_serviceProvider.GetRequiredService<IFileSystem>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IQueueService>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IProcessRunner>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IPdfTools>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IPythonRunner>());
            Assert.NotNull(_serviceProvider.GetRequiredService<ICustomSendBackDocumentGeneratorService>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IInventoryDriverRecordMessageProcessor>());
            Assert.NotNull(_serviceProvider.GetRequiredService<ISendBackCheckXmlFileWriter>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IEdi835MessagePublisher>());
            Assert.NotNull(_serviceProvider.GetRequiredService<MessageProcessor>());
        }
    }
}
