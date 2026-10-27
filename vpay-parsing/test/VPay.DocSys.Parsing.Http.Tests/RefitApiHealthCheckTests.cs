using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using VPay.DocSys.Parsing.Http.Abstractions.PlatformConfiguration;
using Xunit;

namespace VPay.DocSys.Parsing.Http.Tests
{
    public class RefitApiHealthCheckTests
    {
        [Theory]
        [InlineData(HealthStatus.Unhealthy)]
        [InlineData(HealthStatus.Degraded)]
        [InlineData(HealthStatus.Healthy)]
        public async Task CheckHealthAsync_ShouldBeExpected(HealthStatus expected)
        {
            // arrange
            var mockApi = new Mock<IPlatformConfigurationApi>();
            var check = new RefitApiHealthCheck<IPlatformConfigurationApi>(mockApi.Object,
                async a => await Task.FromResult(expected));

            // act
            var result = await check.CheckHealthAsync(new HealthCheckContext());

            // assert
            Assert.Equal(expected, result.Status);
        }

        [Fact]
        public async Task CheckHealthAsync_ShouldBeUnhealthy()
        {
            // arrange
            var mockApi = new Mock<IPlatformConfigurationApi>();
            var check = new RefitApiHealthCheck<IPlatformConfigurationApi>(mockApi.Object,
                a => throw new Exception("failure"));

            // act
            var result = await check.CheckHealthAsync(new HealthCheckContext());

            // assert
            Assert.Equal(HealthStatus.Unhealthy, result.Status);
        }
    }
}
