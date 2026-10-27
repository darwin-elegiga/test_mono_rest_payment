using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using VPay.Optum.PaymentRouting.Data.Abstractions;
using VPay.Optum.PaymentRouting.Data.Sql.Health;
using Xunit;

namespace VPay.Optum.PaymentRouting.Data.Sql.Tests.Health
{
    public class SqlServerSystemHealthCheckFacts
    {
        public sealed class CheckHealthAsync : SqlServerSystemHealthCheckFacts
        {
            private readonly Mock<IDataConnection<SqlConnection>> _connection;
            private readonly SqlServerSystemHealthCheck _healthCheck;

            public CheckHealthAsync()
            {
                _connection = new Mock<IDataConnection<SqlConnection>>();
                _healthCheck = new SqlServerSystemHealthCheck(_connection.Object);
            }

            [Fact]
            public async Task WhenUnhealthy_ShoulderReturnConnectionFailed()
            {
                // arrange
                var context = new HealthCheckContext();
                var expectedResult = HealthCheckResult.Unhealthy("Connection Failed");
                _connection.Setup(conn => conn.CanConnectAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(false);

                // act
                HealthCheckResult result = await _healthCheck.CheckHealthAsync(context);

                // assert
                result.Should().Be(expectedResult);
            }

            [Fact]
            public async Task WhenHealthy_ShoulderReturnConnectionSucceeded()
            {
                // arrange
                var context = new HealthCheckContext();
                var expectedResult = HealthCheckResult.Healthy("Connection Succeeded");
                _connection.Setup(conn => conn.CanConnectAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true);

                // act
                HealthCheckResult result = await _healthCheck.CheckHealthAsync(context);

                // assert
                result.Should().Be(expectedResult);
            }
        }
    }
}
