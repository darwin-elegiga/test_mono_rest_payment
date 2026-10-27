using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Xunit;

namespace VPay.Optum.Http.Loaf.Tests
{
    public class LoafApiConfigurationExtensionsTests
    {
        [Fact]
        public void AddLoafApiClient_ShouldConfigureHttpClient()
        {
            // Arrange
            var services = new ServiceCollection();
            var config = new LoafApiHttpClientConfig
            {
                BaseAddress = new Uri("https://example.com"),
                Timeout = TimeSpan.FromSeconds(30),
                UserName = "testuser",
                Password = "testpassword"
            };

            // Act
            services.AddLoafApiClient(options =>
            {
                options.BaseAddress = config.BaseAddress;
                options.Timeout = config.Timeout;
                options.UserName = config.UserName;
                options.Password = config.Password;
            });

            var serviceProvider = services.BuildServiceProvider();
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var options = serviceProvider.GetRequiredService<IOptions<LoafApiHttpClientConfig>>().Value;

            // Assert
            Assert.NotNull(httpClientFactory);
            Assert.Equal(config.BaseAddress, options.BaseAddress);
            Assert.Equal(config.Timeout, options.Timeout);
            Assert.Equal(config.UserName, options.UserName);
            Assert.Equal(config.Password, options.Password);
        }

        [Fact]
        public void AddLoafApiHealthCheck_ShouldRegisterHealthCheck()
        {
            // Arrange
            var services = new ServiceCollection();
            var healthChecksBuilder = services.AddHealthChecks();

            // Act
            healthChecksBuilder.AddLoafApiHealthCheck();
            var serviceProvider = services.BuildServiceProvider();
            var healthCheckOptions = serviceProvider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;

            // Assert
            Assert.Contains(healthCheckOptions.Registrations, registration => registration.Name == "uri-loaf-api");
        }
    }
}
