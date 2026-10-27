using FluentAssertions;

using Microsoft.Extensions.Logging;

using Moq;

using System;
using System.IO;
using System.IO.Abstractions;
using System.Threading.Tasks;

using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Configs;

using Xunit;

namespace VPay.DocSys.Generation.Tests
{
    public class DropToPrintServiceTests
    {
        protected readonly Mock<IProcessRunner> _processRunner = new Mock<IProcessRunner>();
        protected readonly Mock<IFileSystem> _fileSystem = new Mock<IFileSystem>();
        protected readonly Mock<ILogger<DropToPrintService>> _logger = new Mock<ILogger<DropToPrintService>>();
        protected readonly Mock<IPath> _path = new Mock<IPath>();

        protected readonly int ErrorExitCode = -1;
        protected readonly int SuccessExitCode = 0;
        protected readonly string ValidFilePath = "/ValidFilePath/year/month/day/statusFile.sts";

        protected DropToPrintSettings Configuration;
        protected string StatusFilePath;

        protected IDropToPrintService SUT => new DropToPrintService(
            _processRunner.Object,
            _fileSystem.Object,
            _logger.Object,
            Configuration
            );

        public DropToPrintServiceTests()
        {
            StatusFilePath = ValidFilePath;            

            _path
                .Setup(_ => _.GetDirectoryName(It.IsAny<string>()))
                .Returns(Path.GetDirectoryName(StatusFilePath));

            Configuration = new DropToPrintSettings
            {
                PythonScript = It.IsAny<string>()
            };
        }
    }

    public class WhenTestingCallDropToPrintPython : DropToPrintServiceTests
    {
        [Fact]
        public async ValueTask ShouldBeCorrectExitCode()
        {
            // Arrange
            _processRunner
                .Setup(_ => _.RunProcessAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(Task.FromResult(new ProcessResult()));

            // Act
            var result = await Act();

            // Assert
            result.Should().NotBe(ErrorExitCode);
            result.Should().Be(SuccessExitCode);
        }

        [Fact]
        public async ValueTask ShouldThrowExceptionForRunProcessAsync()
        {
            // Arrange
            _processRunner
                .Setup(x => x.RunProcessAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string[]>()))
                .ThrowsAsync(new ProcessRunnerException(ErrorExitCode, "Exception thrown from payment_to_mail.py"));

            // Act
            var result = await Assert.ThrowsAsync<Exception>(() => Act());

            // Assert
            result.Should().BeOfType<Exception>()
                .Which.Message.Should().Be($"Exception for sts file: {StatusFilePath}");

            result.InnerException.Should().BeOfType<ProcessRunnerException>()
                .Which.Message.Should().Be("Exception thrown from payment_to_mail.py");

            result.InnerException.Should().BeOfType<ProcessRunnerException>()
                .Which.ExitCode.Should().Be(ErrorExitCode);
        }

        private async Task<int> Act()
        {
            return await SUT.CallDropToPrintPython(StatusFilePath);
        }
    }
}
