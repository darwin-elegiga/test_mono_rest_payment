using System;
using System.IO.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;
using Xunit;


namespace VPay.DocSys.Generation.FaxMessage.ErrorQueueProcessor.Tests
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
                .AddSingleton<FaxMessageErrorProcessorService>()
                .BuildServiceProvider();
        }

        [Fact]
        public void AddServices_ShouldInjectForReminderFaxProcessorService()
        {
            Assert.NotNull(_serviceProvider.GetRequiredService<IQueueService>());
            Assert.NotNull(_serviceProvider.GetRequiredService<ApiSettings>());
            Assert.NotNull(_serviceProvider.GetRequiredService<Models.ProcessorSettings>());
            Assert.NotNull(_serviceProvider.GetRequiredService<Storage.IStorageSettings>());
            Assert.NotNull(_serviceProvider.GetRequiredService<Models.QueueConfigurations>().FaxMessageQueue);
            // ReceiverFactory.GetReceiver is static, so we can't get FaxMessageErrorProcessorService or MessageProcessor

            Assert.NotNull(_serviceProvider.GetRequiredService<IFaxMessageErrorQueueMessageProcessor>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IFaxMessageErrorQueueMessageProcessor>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IFileSystem>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IQueueService>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IProcessRunner>());

        }
    }
}
