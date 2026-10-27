using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using VPay.DocSys.Parsing.Common.Interfaces;
using Xunit;

namespace VPay.DocSys.Parsing.OutboundRedCardDataQueueProcessor.Tests
{
    public class StartupTests
    {
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;

        public StartupTests()
        {
            _configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true)
                .AddJsonFile("appsettings.Development.json", true)
                .Build();
            _serviceProvider = new ServiceCollection()
                .AddSingleton<IConfiguration>(_configuration)
                .AddLogging(b => b.AddProvider(NullLoggerProvider.Instance))
                .AddServices(_configuration)
                .BuildServiceProvider();
        }

        [Fact]
        public void AddServices_ShouldRegisterMessageProcessor()
        {
            // arrange
            using var scope = _serviceProvider.CreateScope();

            // act
            var messageProcessor = _serviceProvider.GetRequiredService<OutboundRedCardDataQueueProcessor.MessageProcessor>();

            // see MessageProcessor line 42
            var queueSettings = scope.ServiceProvider.GetRequiredService<OutboundRedCardDataQueueSettings>();
            // see MessageProcessor line 102
            var processingService = scope.ServiceProvider.GetRequiredService<IOutboundRedCardDataDriverFileService>();

            // assert
            Assert.NotNull(messageProcessor);
            Assert.NotNull(queueSettings);
            Assert.NotNull(processingService);
        }
    }
}
