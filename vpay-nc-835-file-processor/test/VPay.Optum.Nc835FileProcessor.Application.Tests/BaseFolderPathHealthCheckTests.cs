using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Moq;
using VPay.Optum.Nc835FileProcessor.Application.HealthChecks;
using VPay.Optum.Nc835FileProcessor.Application.Models;
using Xunit;

namespace VPay.Optum.Nc835FileProcessor.Application.Tests.HealthChecks
{
    public class BaseFolderPathHealthCheckTests
    {
        private readonly Mock<IFileSystem> _mockFileSystem;
        private readonly Mock<ILogger<BaseFolderPathHealthCheck>> _mockLogger;
        private readonly Nc835FileSettings _settings;

        public BaseFolderPathHealthCheckTests()
        {
            _mockFileSystem = new Mock<IFileSystem>();
            _mockLogger = new Mock<ILogger<BaseFolderPathHealthCheck>>();
            _settings = new Nc835FileSettings { BaseFolderPath = "C:\\TestFolder" };
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenSettingsIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new BaseFolderPathHealthCheck(null!, _mockFileSystem.Object, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenFileSystemIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new BaseFolderPathHealthCheck(_settings, null!, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new BaseFolderPathHealthCheck(_settings, _mockFileSystem.Object, null!));
        }

        [Fact]
        public async Task CheckHealthAsync_ShouldReturnHealthy_WhenBaseFolderPathExists()
        {
            // Arrange
            _mockFileSystem.Setup(fs => fs.Directory.Exists(_settings.BaseFolderPath)).Returns(true);
            var healthCheck = new BaseFolderPathHealthCheck(_settings, _mockFileSystem.Object, _mockLogger.Object);
            var context = new HealthCheckContext();

            // Act
            var result = await healthCheck.CheckHealthAsync(context);

            // Assert
            Assert.Equal(HealthStatus.Healthy, result.Status);
            Assert.Equal($"{nameof(_settings.BaseFolderPath)} found", result.Description);
        }

        [Fact]
        public async Task CheckHealthAsync_ShouldThrowDirectoryNotFoundException_WhenBaseFolderPathDoesNotExist()
        {
            // Arrange
            _mockFileSystem.Setup(fs => fs.Directory.Exists(_settings.BaseFolderPath)).Returns(false);
            var healthCheck = new BaseFolderPathHealthCheck(_settings, _mockFileSystem.Object, _mockLogger.Object);
            var context = new HealthCheckContext();

            // Act & Assert
            await Assert.ThrowsAsync<DirectoryNotFoundException>(() => healthCheck.CheckHealthAsync(context));
        }
    }
}
