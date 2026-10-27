using System;
using System.IO.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Models;
using VPay.DocSys.Pdf.Tools;
using Xunit;


namespace VPay.DocSys.Generation.FaxRetry.QueueProcessor.Tests
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
                .AddSingleton<FaxRetryProcessorService>()
                .BuildServiceProvider();
        }

        [Fact]
        public void AddServices_ShouldInjectForReminderFaxProcessorService()
        {
            Assert.NotNull(_serviceProvider.GetRequiredService<ApiSettings>());
            Assert.NotNull(_serviceProvider.GetRequiredService<ProcessorSettings>());
            Assert.NotNull(_serviceProvider.GetRequiredService<Storage.IStorageSettings>());
            Assert.NotNull(_serviceProvider.GetRequiredService<QueueConfigurations>().FaxRetryQueue);
            // ReceiverFactory.GetReceiver is static, so we can't get FaxRetryProcessorService or MessageProcessor

            Assert.NotNull(_serviceProvider.GetRequiredService<IFileSystem>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IQueueService>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IProcessRunner>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IPdfTools>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IFaxMessageXmlReader>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IFaxArchiveMessageProcessor>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IEftFaxPdfCreator>());
            Assert.NotNull(_serviceProvider.GetRequiredService<ISendBackCheckXmlFileWriter>());
            Assert.NotNull(_serviceProvider.GetRequiredService<EftFaxFopXsl>());
            Assert.NotNull(_serviceProvider.GetRequiredService<FopBaseXsl>());
            Assert.NotNull(_serviceProvider.GetRequiredService<ReminderFaxBaseXsl>());
        }
    }
}
