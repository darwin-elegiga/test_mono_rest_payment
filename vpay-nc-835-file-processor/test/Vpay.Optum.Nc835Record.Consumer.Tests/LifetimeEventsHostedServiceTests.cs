using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using VPay.Optum.Nc835Record.Consumer;
using Xunit;

namespace VPay.Optum.Nc835Record.Consumer.Tests
{
    public class LifetimeEventsHostedServiceTests
    {
        private readonly Mock<ILogger<LifetimeEventsHostedService>> _mockLogger;
        private readonly Mock<IHostApplicationLifetime> _mockHostLifetime;
        private readonly LifetimeEventsHostedService _service;

        public LifetimeEventsHostedServiceTests()
        {
            _mockLogger = new Mock<ILogger<LifetimeEventsHostedService>>();
            _mockHostLifetime = new Mock<IHostApplicationLifetime>();
            _service = new LifetimeEventsHostedService(_mockLogger.Object, _mockHostLifetime.Object);
        }



        [Fact]
        public async Task StopAsync_CompletesSuccessfully()
        {
            // Act
            await _service.StopAsync(CancellationToken.None);

            // Assert
            Assert.Equal(Task.CompletedTask, Task.CompletedTask);
        }

        [Fact]
        public void OnStarted_LogsInformation()
        {
            // Act
            _service.GetType()
                .GetMethod("OnStarted", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(_service, new object[] { _mockLogger.Object });

            // Assert
            _mockLogger.Verify(
                logger => logger.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Startup occurred")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void OnStopping_LogsInformation()
        {
            // Act
            _service.GetType()
                .GetMethod("OnStopping", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(_service, new object[] { _mockLogger.Object });

            // Assert
            _mockLogger.Verify(
                logger => logger.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Shutdown occurred")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void OnStopped_LogsInformation()
        {
            // Act
            _service.GetType()
                .GetMethod("OnStopped", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(_service, new object[] { _mockLogger.Object });

            // Assert
            _mockLogger.Verify(
                logger => logger.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Shutdown completed")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
        public class TestHostApplicationLifetime : IHostApplicationLifetime
        {
            private readonly CancellationTokenSource _applicationStartedSource = new();
            private readonly CancellationTokenSource _applicationStoppingSource = new();
            private readonly CancellationTokenSource _applicationStoppedSource = new();

            public CancellationToken ApplicationStarted => _applicationStartedSource.Token;
            public CancellationToken ApplicationStopping => _applicationStoppingSource.Token;
            public CancellationToken ApplicationStopped => _applicationStoppedSource.Token;

            public void TriggerApplicationStarted() => _applicationStartedSource.Cancel();
            public void TriggerApplicationStopping() => _applicationStoppingSource.Cancel();
            public void TriggerApplicationStopped() => _applicationStoppedSource.Cancel();

            // Implementing the missing interface member
            public void StopApplication()
            {
                // This method can be left empty or include logic as needed for testing purposes.
            }
        }
    }
}
