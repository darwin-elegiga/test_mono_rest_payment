using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using MassTransit;
using MediatR;
using VPay.MassTransit.Abstractions;
using Microsoft.Extensions.Options;
using VPay.Optum.Nc835Record.Application.Models;

namespace VPay.Optum.Nc835Record.Consumer.Tests
{
    public class StartupTests
    {
        [Fact]
        public void ConfigureServices_ShouldRegisterAllServices()
        {
            // Arrange
            var services = new ServiceCollection();
            var configurationMock = new Mock<IConfiguration>();
            var hostContext = new HostBuilderContext(new Dictionary<object, object>()) // Fix: Changed Dictionary<string, object> to Dictionary<object, object>
            {
                Configuration = configurationMock.Object
            };

            // Fix: Correctly create a mock instance and pass its Object property
            var hostApplicationLifetimeMock = new Mock<IHostApplicationLifetime>();
            services.AddSingleton<IHostApplicationLifetime>(hostApplicationLifetimeMock.Object);

            configurationMock.Setup(c => c.GetSection("Settings")).Returns(new Mock<IConfigurationSection>().Object);
            configurationMock.Setup(c => c.GetSection("RabbitQueue")).Returns(new Mock<IConfigurationSection>().Object);
            configurationMock.Setup(c => c.GetSection("OptumIntegrationDatabase")).Returns(new Mock<IConfigurationSection>().Object);

            // Act
            Startup.ConfigureServices(hostContext, services);

            // Assert
            var serviceProvider = services.BuildServiceProvider();

            // Example assertions to verify specific services are registered
            Assert.NotNull(serviceProvider.GetService<IMediator>());
            Assert.NotNull(serviceProvider.GetService<IHostedService>());
            Assert.NotNull(serviceProvider.GetService<IOptions<Settings>>());
            Assert.NotNull(serviceProvider.GetService<IOptions<TransportConfig>>());
        }
    }
}
