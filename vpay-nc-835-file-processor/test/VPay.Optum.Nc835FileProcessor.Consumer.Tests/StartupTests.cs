using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using VPay.MassTransit.Abstractions;
using VPay.Optum.Nc835FileProcessor.Application.Models;
using VPay.Optum.Nc835FileProcessor.Application.Services;
using VPay.Optum.Nc835FileProcessor.Consumer;
using Xunit;

namespace VPay.Optum.Nc835FileProcessor.Consumer.Tests
{
    public class StartupTests
    {
        [Fact]
        public void ConfigureServices_RegistersExpectedServices()
        {
            // Arrange
            var services = new ServiceCollection();
            var configurationData = new Dictionary<string, string>
               {
                   { "Settings:BaseFolderPath", "SomeValue" },
                   { "TransportConfig:PrimaryHostName", "localhost" },
                   { "OptumIntegrationDatabase:ConnectionString", "Server=.;Database=TestDb;Trusted_Connection=True;" }
               };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configurationData!)
                .Build();

            var hostContext = new HostBuilderContext(new Dictionary<object, object>())
            {
                Configuration = configuration
            };
            services.Configure<Nc835FileSettings>(configuration.GetSection("Settings"));
            services.AddLogging();
            
            // Act
            Startup.ConfigureServices(hostContext, services);

            // Assert
            var serviceProvider = services.BuildServiceProvider();

            // Verify options are configured
            var nc835FileSettings = serviceProvider.GetService<Nc835FileSettings>();
            Assert.NotNull(nc835FileSettings);
            Assert.Equal("SomeValue", nc835FileSettings.BaseFolderPath);

            var transportConfig = serviceProvider.GetService<TransportConfig>();
            Assert.NotNull(transportConfig);

            // Verify specific services are registered
            Assert.NotNull(serviceProvider.GetService<IFileSystem>());
            Assert.NotNull(serviceProvider.GetService<IX12Parser>());

           
        }
    }
}
