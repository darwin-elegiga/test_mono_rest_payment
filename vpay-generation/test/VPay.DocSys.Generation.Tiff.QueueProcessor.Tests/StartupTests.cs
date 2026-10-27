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

namespace VPay.DocSys.Generation.Tiff.QueueProcessor.Tests
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
                .AddSingleton<TiffProcessorService>()
                .BuildServiceProvider();
        }

        [Fact]
        public void AddServices_ShouldInjectForTiffProcessorService()
        {
            Assert.NotNull(_serviceProvider.GetRequiredService<DocSysSettings>());
            Assert.NotNull(_serviceProvider.GetRequiredService<ProcessorSettings>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IStorageSettings>());
            Assert.NotNull(_serviceProvider.GetRequiredService<FaxJobSettings>());
            Assert.NotNull(_serviceProvider.GetRequiredService<FaxBase>());
            Assert.NotNull(_serviceProvider.GetRequiredService<QueueConfigurations>().TiffQueue);
            // ReceiverFactory.GetReceiver is static, so we can't get TiffProcessorService or MessageProcessor

            Assert.NotNull(_serviceProvider.GetRequiredService<IFileSystem>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IProcessRunner>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IPdfTools>());
            Assert.NotNull(_serviceProvider.GetRequiredService<ITiffOperations>());
            Assert.NotNull(_serviceProvider.GetRequiredService<ITiffMessageQueuer>());
            Assert.NotNull(_serviceProvider.GetRequiredService<ITiffQueueMessageProcessor>());
            Assert.NotNull(_serviceProvider.GetRequiredService<ITiffErrorQueueMessageProcessor>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IQueueService>());
        }
    }
}
