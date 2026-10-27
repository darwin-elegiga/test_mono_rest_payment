using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VPay.DocSys.Parsing.Api.Controllers;
using VPay.DocSys.Parsing.Common;
using Xunit;

namespace VPay.DocSys.Parsing.Api.Tests
{
    public class StartupTests
    {
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;
        private readonly System.IO.Stream _stream;

        public StartupTests()
        {
            // setup mocks
            var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { "/home/bogner/.ssh/id_rsa", new MockFileData("FAKE") }
            });
            var mockRCRepo = new Mock<Data.IRedCardRepository>();


            // setup SUT
            _configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true)
                .AddJsonFile("appsettings.Development.json", true)
                .Build();
            var startup = new VPay.DocSys.Parsing.Api.Startup(_configuration);
            var collection = new ServiceCollection()
                .AddSingleton<IConfiguration>(_configuration)
                .AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));
            startup.ConfigureServices(collection);
            _serviceProvider = collection
                .AddSingleton<IFileSystem>(mockFileSystem)
                .AddTransient(s => mockRCRepo.Object)
                .BuildServiceProvider();
        }
        ~StartupTests()
        {
            _stream?.Close();
            _stream?.Dispose();
        }

        [Fact]
        public void AddServices_ShouldInjectForAboutController()
            => Assert.NotNull(new AboutController(
                _serviceProvider.GetRequiredService<AboutInfo>()));

        [Fact]
        public void AddServices_ShouldInjectForConfigController()
            => Assert.NotNull(new ConfigController(
                _serviceProvider.GetRequiredService<DocSys.Common.IDocSysConfigService>(),
                _serviceProvider.GetRequiredService<Common.Interfaces.IXmlConfigParserService>(),
                _serviceProvider.GetRequiredService<Http.Abstractions.PlatformConfiguration.IPlatformConfigurationApi>(),
                _serviceProvider.GetRequiredService<ILogger<ConfigController>>()));

        [Fact]
        public void AddServices_ShouldInjectForEdi835SupportController()
            => Assert.NotNull(new Edi835SupportController(
                _serviceProvider.GetRequiredService<IStatusFilePathHelper>(),
                _serviceProvider.GetRequiredService<Common.IEdi835TarService>(),
                _serviceProvider.GetRequiredService<Data.IEdi835Repository>(),
                _serviceProvider.GetRequiredService<Edi835GenerationQueueSettings>(),
                _serviceProvider.GetRequiredService<Common.Edi835ConfigurationCache>(),
                _serviceProvider.GetRequiredService<Common.IInbound835FileService>(),
                _serviceProvider.GetRequiredService<ILoggerFactory>()));

        [Fact]
        public void AddServices_ShouldInjectForHealthCheckController()
            => Assert.NotNull(new HealthCheckController(
                _serviceProvider.GetRequiredService<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService>()));

        [Fact]
        public void AddServices_ShouldInjectForInbound835FileController()
            => Assert.NotNull(new Inbound835FileController(
                _serviceProvider.GetRequiredService<Common.IInbound835FileService>()));

        [Fact]
        public void AddServices_ShouldInjectForInventoryDriverFileController()
            => Assert.NotNull(new InventoryDriverFileController(
                _serviceProvider.GetRequiredService<Common.Interfaces.IInventoryDriverFileService>()));

        [Fact]
        public void AddServices_ShouldInjectForOutboundRedCardDataDriverFileController()
            => Assert.NotNull(new OutboundRedCardDataDriverFileController(
                _serviceProvider.GetRequiredService<Common.Interfaces.IOutboundRedCardDataDriverFileService>()));

        [Fact]
        public void AddServices_ShouldInjectForSshTestsController()
            => Assert.NotNull(new SshTestsController(
                _serviceProvider.GetRequiredService<System.IO.Abstractions.IFileSystem>(),
                _serviceProvider.GetRequiredService<IConfiguration>(),
                _serviceProvider.GetRequiredService<Extensions.Ssh.ISshClientFactory>()));

        [Fact]
        public void AddServices_ShouldInjectForStatusFileController()
            => Assert.NotNull(new StatusFileController(
                _serviceProvider.GetRequiredService<Common.Interfaces.IStatusFileService>(),
                _serviceProvider.GetRequiredService<Common.Interfaces.IStatusFileTracker>()));

        [Fact]
        public void AddServices_ShouldInjectForSupportController()
            => Assert.NotNull(new SupportController(
                _serviceProvider.GetRequiredService<Common.Interfaces.IStatusFileTracker>(),
                _serviceProvider.GetRequiredService<Common.Interfaces.IDriverFileService>(),
                _serviceProvider.GetRequiredService<Common.Interfaces.IStatusFileService>(),
                _serviceProvider.GetRequiredService<Data.IRedCardRepository>(),
                _serviceProvider.GetRequiredService<Common.Interfaces.IArchiveRecordService>()));
    }
}
