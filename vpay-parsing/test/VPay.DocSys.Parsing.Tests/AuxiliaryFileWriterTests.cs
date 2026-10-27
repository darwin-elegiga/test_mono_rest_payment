using System;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using VPay.DocSys.Common;
using VPay.DocSys.Data;
using VPay.DocSys.Parsing.Common;
using Xunit;

namespace VPay.DocSys.Parsing.Tests
{
    public class AuxiliaryFileWriterTests : BaseStatsFileTests
    {
        private IFileSystem _fileSystem;

        [Fact]
        public async Task GetFileHasStatusReturnsTrue_ShouldThrowException()
        {
            var statusFileName = Path.Combine(Path.GetTempPath(), "GetFileHasStatusReturnsTrue.sts");
            SetupInitialMocks(statusFileName);
            RedCardRepositoryMock = SetupRedCardRepository(true);

            var auxiliaryFileWriter = new AuxiliaryFileWriter(LoggerFactory.CreateLogger<AuxiliaryFileWriter>(),
                RedCardRepositoryMock.Object, _fileSystem);

            TestFiles.Add(statusFileName);
            await Assert.ThrowsAsync<Exception>(() => auxiliaryFileWriter.Write(statusFileName, 1, new StatusFile(StatusFileData), false, true, false));
        }

        [Fact]
        public async Task GetFileHasStatusThrowsException_ShouldThrowException()
        {
            var statusFileName = Path.Combine(Path.GetTempPath(), "GetFileHasStatusThrowsException.sts");
            SetupInitialMocks(statusFileName);
            RedCardRepositoryMock.Setup(x =>
                    x.GetFileHasStatus(It.IsAny<long>(), It.IsAny<RedCardFileStatusCode>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("test"));

            var auxiliaryFileWriter = new AuxiliaryFileWriter(LoggerFactory.CreateLogger<AuxiliaryFileWriter>(),
                RedCardRepositoryMock.Object, _fileSystem);

            TestFiles.Add(statusFileName);
            await Assert.ThrowsAsync<Exception>(() => auxiliaryFileWriter.Write(statusFileName, 1, new StatusFile(StatusFileData), false, true, false));
        }

        [Theory]
        [InlineData(false, false, false)]
        [InlineData(false, false, true)]
        [InlineData(false, true, false)]
        [InlineData(false, true, true)]
        [InlineData(true, false, false)]
        [InlineData(true, false, true)]
        [InlineData(true, true, false)]
        [InlineData(true, true, true)]
        public async Task ShouldRunAsExpected(bool isReissue, bool checkAuxFileGenerated, bool updateStatus)
        {
            var statusFileName = Path.Combine(Path.GetTempPath(),
                $"ShouldRun{(isReissue ? "0" : "1")}{(checkAuxFileGenerated ? "0" : "1")}{(updateStatus ? "0" : "1")}.sts");
            SetupInitialMocks(statusFileName);
            var auxiliaryFileWriter = new AuxiliaryFileWriter(LoggerFactory.CreateLogger<AuxiliaryFileWriter>(),
                RedCardRepositoryMock.Object, _fileSystem);

            TestFiles.Add(statusFileName);

            var response = await auxiliaryFileWriter.Write(statusFileName, 1, new StatusFile(StatusFileData), isReissue,
                checkAuxFileGenerated, updateStatus);
            response.Should().Be(Path.ChangeExtension(statusFileName, ".aux"));
            VerifyRedCardGetFileHasStatusCallTimes(RedCardRepositoryMock, checkAuxFileGenerated ? 1 : 0);
            VerifyRedCardUpdateFileStatusCallTimes(RedCardRepositoryMock, updateStatus ? 1 : 0);
            VerifyReissue(response, isReissue);
        }

        [Fact]
        public async Task UpdateFileStatusThrowsError_ShouldThrowException()
        {
            var statusFileName = Path.Combine(Path.GetTempPath(), "UpdateFileStatusThrowsError.sts");
            SetupInitialMocks(statusFileName);
            RedCardRepositoryMock.Setup(x =>
                    x.UpdateFileStatus(It.IsAny<long>(), It.IsAny<RedCardFileStatusCode>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("test"));

            var auxiliaryFileWriter = new AuxiliaryFileWriter(LoggerFactory.CreateLogger<AuxiliaryFileWriter>(),
                RedCardRepositoryMock.Object, _fileSystem);

            TestFiles.Add(statusFileName);
            await Assert.ThrowsAsync<Exception>(() => auxiliaryFileWriter.Write(statusFileName, 1, new StatusFile(StatusFileData), false, false));
        }

        protected override void SetupInitialMocks(string stsFilePath)
        {
            base.SetupInitialMocks(stsFilePath);

            _fileSystem = new FileSystem();
        }

        private static void VerifyReissue(string auxFileName, bool isReissue)
        {
            Assert.True(File.Exists(auxFileName));
            var fileData = File.ReadAllLines(auxFileName).ToList();
            var auxLine = fileData.FirstOrDefault(x => x.StartsWith("33"));
            auxLine.Should().NotBeNullOrWhiteSpace();

            var splitData = auxLine.Split('|');
            splitData.Length.Should().Be(7);
            var coverName = splitData[3];
            coverName.Should().NotBeNullOrWhiteSpace();
            coverName.Should().Contain(".");
            var coverNameStart = coverName.Split('.')[0];
            if (isReissue)
            {
                Assert.False(long.TryParse(coverNameStart, out _), "Start of CoverName for Reissues should not be numeric");
            }
            else
            {
                Assert.True(long.TryParse(coverNameStart, out _), "Start of CoverName for Non Reissues should be numeric");
            }
        }
    }
}
