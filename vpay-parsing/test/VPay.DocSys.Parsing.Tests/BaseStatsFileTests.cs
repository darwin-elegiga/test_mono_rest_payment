using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VPay.DocSys.Data;

namespace VPay.DocSys.Parsing.Tests
{
    public abstract class BaseStatsFileTests : IDisposable
    {
        protected readonly List<string> TestFiles = new List<string>();
        protected ILoggerFactory LoggerFactory;
        protected Mock<IRedCardRepository> RedCardRepositoryMock;
        protected static string[] StatusFileData => File.ReadAllLines("statusFileTestData/B001809287_FS3U201935.sts");
        protected static string[] PEStatusFileData => File.ReadAllLines("statusFileTestData/B001809287_FS3U201935_PE.sts");
        protected static string[] StpStatusFileData => File.ReadAllLines("statusFileTestData/B001809287_FS3U201935_STP.sts");

        protected static ILoggerFactory SetupLoggerFactory()
        {
            return new NullLoggerFactory();
        }

        protected virtual void SetupInitialMocks(string stsFilePath)
        {
            LoggerFactory = SetupLoggerFactory();
            RedCardRepositoryMock = SetupRedCardRepository();
        }

        protected static Mock<IRedCardRepository> SetupRedCardRepository(bool getFileHasStatusResponse = false,
            int updateFileStatusResponse = -1)
        {
            var redCardRepositoryMock = new Mock<IRedCardRepository>();
            redCardRepositoryMock.Setup(x =>
                    x.GetFileHasStatus(It.IsAny<long>(), It.IsAny<RedCardFileStatusCode>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(getFileHasStatusResponse);
            redCardRepositoryMock.Setup(x =>
                    x.UpdateFileStatus(It.IsAny<long>(), It.IsAny<RedCardFileStatusCode>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(updateFileStatusResponse);

            return redCardRepositoryMock;
        }

        protected static void VerifyRedCardGetFileHasStatusCallTimes(Mock<IRedCardRepository> redCardRepositoryMock, int expectedTimesCalled)
        {
            redCardRepositoryMock.Verify(
                x => x.GetFileHasStatus(It.IsAny<long>(), It.IsAny<RedCardFileStatusCode>(),
                    It.IsAny<CancellationToken>()), Times.Exactly(expectedTimesCalled));
        }


        public void Dispose()
        {
            if (TestFiles != null && TestFiles.Any())
            {
                foreach (var testFile in TestFiles)
                {
                    if (File.Exists(testFile))
                        File.Delete(testFile);

                    var auxFileName = Path.ChangeExtension(testFile, ".aux");
                    if (File.Exists(auxFileName))
                        File.Delete(auxFileName);
                }
            }
        }

        protected static void VerifyRedCardUpdateFileStatusCallTimes(Mock<IRedCardRepository> redCardRepositoryMock,
            int expectedTimesCalled)
        {
            redCardRepositoryMock.Verify(
                x => x.UpdateFileStatus(It.IsAny<long>(), It.IsAny<RedCardFileStatusCode>(),
                    It.IsAny<CancellationToken>()), Times.Exactly(expectedTimesCalled));
        }
    }
}
