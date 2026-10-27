using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using VPay.DocSys.Parsing.Common;
using Xunit;

namespace VPay.DocSys.Parsing.Inbound835FileQueueProcessor.Tests
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
            var messageProcessor = _serviceProvider.GetRequiredService<Inbound835FileQueueProcessor.MessageProcessor>();
            // see MessageProcessor line 41
            var queueSettings = scope.ServiceProvider.GetRequiredService<Inbound835FileQueueSettings>();
            // see MessageProcessor line 76
            var inbound835FileService = scope.ServiceProvider.GetRequiredService<IInbound835FileService>();

            // assert
            Assert.NotNull(messageProcessor);
            Assert.NotNull(queueSettings);
            Assert.NotNull(inbound835FileService);
        }
    }
}
