using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using VPay.DocSys.Common;
using VPay.DocSys.Data;
using VPay.DocSys.Parsing.Common;
using VPay.DocSys.Parsing.Common.Interfaces;
using VPay.DocSys.Parsing.Common.Rabbitmq;
using VPay.DocSys.Parsing.Http.Abstractions.PlatformConfiguration;
using Xunit;

namespace VPay.DocSys.Parsing.Tests
{
    /// <summary>
    /// Tests for <see cref="StatusFileService.RegenerateAuxFile(string)"/> method of <see cref="StatusFileService"/> class.
    /// </summary>
    [Collection(nameof(StatusFileService))] // Required to run tests sequentially due to shared file system test files.
    public class StatusFileService_RegenerateAuxFile_Tests : BaseStatusFileServiceTests
    {
        [Fact]
        public async Task AuxiliaryFileWriterThrowsException_ThrowsException()
        {
            var statusFileName = Path.Combine(Path.GetTempPath(), "ATest.bad");
            SetupInitialMocks(statusFileName);

            _auxiliaryFileWriterMock.Setup(x => x.Write(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<StatusFile>(),
                    It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .ThrowsAsync(new Exception("Test"));

            var statusFileService = CreateStatusFileService();

            TestFiles.Add(statusFileName);
            File.WriteAllLines(statusFileName, StatusFileData);

            await Assert.ThrowsAsync<Exception>(() => statusFileService.RegenerateAuxFile(statusFileName));

            VerifyRedCardUpdateFileStatusCallTimes(RedCardRepositoryMock, 0);
            VerifyAuxiliaryFileWriterWriteCallTimes(_auxiliaryFileWriterMock, 1);
        }

        [Fact]
        public async Task DropToPrintFiles_ShouldThrowNotSupportedError()
        {
            var statusFileName = Path.Combine(Path.GetTempPath(), "PFTest.sts");
            SetupInitialMocks(statusFileName);

            var statusFileService = CreateStatusFileService();

            TestFiles.Add(statusFileName);
            File.WriteAllLines(statusFileName, StatusFileData);
            await Assert.ThrowsAsync<NotSupportedException>(() => statusFileService.RegenerateAuxFile(statusFileName));

            VerifyRedCardUpdateFileStatusCallTimes(RedCardRepositoryMock, 0);
            VerifyAuxiliaryFileWriterWriteCallTimes(_auxiliaryFileWriterMock, 0);
        }

        [Theory]
        [InlineData("")]
        [InlineData((string)null)]
        public async Task EmptyHeaderInputFIle_ShouldThrowArgumentNullException(string stsFileName)
        {
            SetupInitialMocks(stsFileName);

            var statusFileService = CreateStatusFileService();

            await Assert.ThrowsAsync<ArgumentNullException>(() => statusFileService.RegenerateAuxFile(stsFileName));

            VerifyRedCardUpdateFileStatusCallTimes(RedCardRepositoryMock, 0);
            VerifyAuxiliaryFileWriterWriteCallTimes(_auxiliaryFileWriterMock, 0);
        }

        [Fact]
        public async Task EmptySTSFileName_ShouldUpdateInputFileUsingFileName()
        {
            var statusFileName = Path.Combine(Path.GetTempPath(), "B001809287_FS3U201935.sts");
            SetupInitialMocks(statusFileName);

            var statusFileService = CreateStatusFileService();
            TestFiles.Add(statusFileName);
            var statusFile = new StatusFile(StatusFileData)
            {
                Header = { InputFile = "" }
            };
            var statusFileData = statusFile.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
            File.WriteAllLines(statusFileName, statusFileData);

            await statusFileService.RegenerateAuxFile(statusFileName);

            VerifyRedCardUpdateFileStatusCallTimes(RedCardRepositoryMock, 0);
            VerifyAuxiliaryFileWriterWriteCallTimes(_auxiliaryFileWriterMock, 1);
        }

        [Fact]
        public async Task GetStatusFileSummaryMissingPaymentStatusFileId_ThrowsKeyNotFoundException()
        {
            var statusFileName = Path.Combine(Path.GetTempPath(), "ATest.sts");
            SetupInitialMocks(statusFileName);

            _statusFileTrackerMock.Setup(x => x.GetStatusFileSummary(It.IsAny<string>()))
                .Returns(new Dictionary<string, object>
                {
                    ["SomeOtherField"] = 1L
                });

            var statusFileService = CreateStatusFileService();

            TestFiles.Add(statusFileName);
            File.WriteAllLines(statusFileName, StatusFileData);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => statusFileService.RegenerateAuxFile(statusFileName));

            VerifyRedCardUpdateFileStatusCallTimes(RedCardRepositoryMock, 0);
            VerifyAuxiliaryFileWriterWriteCallTimes(_auxiliaryFileWriterMock, 0);
        }

        [Fact]
        public async Task GetStatusFileSummaryPaymentStatusFileIdNotValid_ThrowsKeyNotFoundException()
        {
            var statusFileName = Path.Combine(Path.GetTempPath(), "ATest.sts");
            SetupInitialMocks(statusFileName);

            _statusFileTrackerMock.Setup(x => x.GetStatusFileSummary(It.IsAny<string>()))
                .Returns(new Dictionary<string, object>
                {
                    ["PaymentStatusFileId"] = "bad"
                });

            var statusFileService = CreateStatusFileService();

            TestFiles.Add(statusFileName);
            File.WriteAllLines(statusFileName, StatusFileData);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => statusFileService.RegenerateAuxFile(statusFileName));

            VerifyRedCardUpdateFileStatusCallTimes(RedCardRepositoryMock, 0);
            VerifyAuxiliaryFileWriterWriteCallTimes(_auxiliaryFileWriterMock, 0);
        }

        [Theory]
        [InlineData(1L)]
        [InlineData(100)]
        public async Task GetStatusFileSummaryPaymentStatusFileIdValid_WorksAsExpected(object fieldIdValue)
        {
            var statusFileName = Path.Combine(Path.GetTempPath(), "ATest.sts");
            SetupInitialMocks(statusFileName);
            SetupStatusFileTracker("PaymentStatusFileId", fieldIdValue);

            var statusFileService = CreateStatusFileService();

            TestFiles.Add(statusFileName);
            File.WriteAllLines(statusFileName, StatusFileData);

            await statusFileService.RegenerateAuxFile(statusFileName);

            VerifyRedCardUpdateFileStatusCallTimes(RedCardRepositoryMock, 0);
            VerifyAuxiliaryFileWriterWriteCallTimes(_auxiliaryFileWriterMock, 1);
        }

        [Theory]
        [MemberData(nameof(GetStatusFileSummaryTestData))]
        public async Task GetStatusFileSummaryReturnsNull_ThrowsFileNotFoundException(
            IDictionary<string, object> getStatusFileData)
        {
            var statusFileName = Path.Combine(Path.GetTempPath(), "ATest.sts");
            SetupInitialMocks(statusFileName);
            _statusFileTrackerMock.Setup(x => x.GetStatusFileSummary(It.IsAny<string>()))
                .Returns(getStatusFileData);

            var statusFileService = CreateStatusFileService();

            TestFiles.Add(statusFileName);
            File.WriteAllLines(statusFileName, StatusFileData);

            await Assert.ThrowsAsync<FileNotFoundException>(() => statusFileService.RegenerateAuxFile(statusFileName));

            VerifyRedCardUpdateFileStatusCallTimes(RedCardRepositoryMock, 0);
            VerifyAuxiliaryFileWriterWriteCallTimes(_auxiliaryFileWriterMock, 0);
        }

        [Fact]
        public async Task MissingSTSFile_ThrowsFileNotFoundException()
        {
            var statusFileName = Path.Combine(Path.GetTempPath(), "ATest.sts");
            SetupInitialMocks(statusFileName);

            var statusFileService = CreateStatusFileService();

            TestFiles.Add(statusFileName);
            if (File.Exists(statusFileName))
            {
                File.Delete(statusFileName);
            }

            await Assert.ThrowsAsync<FileNotFoundException>(() => statusFileService.RegenerateAuxFile(statusFileName));

            VerifyRedCardUpdateFileStatusCallTimes(RedCardRepositoryMock, 0);
            VerifyAuxiliaryFileWriterWriteCallTimes(_auxiliaryFileWriterMock, 0);
        }

        [Fact]
        public async Task NoDetailRecords_ShouldReturnWithNoErrors()
        {
            var statusFileName = Path.Combine(Path.GetTempPath(), "B001809287_FS3U201935.sts");
            SetupInitialMocks(statusFileName);

            var statusFileService = CreateStatusFileService();

            TestFiles.Add(statusFileName);
            var statusFile = new StatusFile(StatusFileData);
            statusFile.Records.Clear();
            var statusFileData = statusFile.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
            File.WriteAllLines(statusFileName, statusFileData);

            await statusFileService.RegenerateAuxFile(statusFileName);

            VerifyRedCardUpdateFileStatusCallTimes(RedCardRepositoryMock, 0);
            VerifyAuxiliaryFileWriterWriteCallTimes(_auxiliaryFileWriterMock, 0);
        }

        [Fact]
        public async Task NotRedCard_ShouldThrowNotSupportedError()
        {
            var statusFileName = Path.Combine(Path.GetTempPath(), "PFTest.sts");
            SetupInitialMocks(statusFileName);

            var statusFileService = CreateStatusFileService();

            TestFiles.Add(statusFileName);
            File.WriteAllLines(statusFileName, NotRedCardStatusFileData);
            await Assert.ThrowsAsync<NotSupportedException>(() => statusFileService.RegenerateAuxFile(statusFileName));

            VerifyRedCardUpdateFileStatusCallTimes(RedCardRepositoryMock, 0);
            VerifyAuxiliaryFileWriterWriteCallTimes(_auxiliaryFileWriterMock, 0);
        }

        [Theory]
        [MemberData(nameof(NullArgumentsParameters))]
        public void NullArguments_ShouldThrowArgumentNullException(IStatusFilePathHelper statusFilePathHelper, IConfiguration configuration,
            IDocSysConfigService docSysConfig, ITarService tarService, IRedCardRepository redCardRepository,
            IAuxiliaryFileWriter auxiliaryFileWriter,
            IDocumentTransactionMessageListBuilder documentTransactionMessageListBuilder,
            IDistributionMessageBuilder distributionMessageBuilder, IStatusFileTracker statusFileTracker,
            IEdi835TarService edi835TarService, IPlatformConfigurationApi platformConfigurationApi,
            IPaymentEngineConfirmationFileService paymentEngineConfirmationFileService, ILoggerFactory loggerFactory,
            IRabbitMqPublisherFactory rabbitMqPublisherFactory)
        {
            Assert.Throws<ArgumentNullException>(() => new StatusFileService(statusFilePathHelper, configuration, docSysConfig, tarService,
                redCardRepository, auxiliaryFileWriter, documentTransactionMessageListBuilder,
                distributionMessageBuilder, statusFileTracker, edi835TarService,
                platformConfigurationApi, paymentEngineConfirmationFileService,
                loggerFactory, rabbitMqPublisherFactory));
        }

        [Fact]
        public async Task ShouldGenerateAuxFileCorrectly()
        {
            var statusFileName = Path.Combine(Path.GetTempPath(), "ATest.sts");
            SetupInitialMocks(statusFileName);

            var statusFileService = CreateStatusFileService();

            TestFiles.Add(statusFileName);
            File.WriteAllLines(statusFileName, StatusFileData);
            await statusFileService.RegenerateAuxFile(statusFileName);

            VerifyRedCardUpdateFileStatusCallTimes(RedCardRepositoryMock, 0);
            VerifyAuxiliaryFileWriterWriteCallTimes(_auxiliaryFileWriterMock, 1);
        }
    }
}
