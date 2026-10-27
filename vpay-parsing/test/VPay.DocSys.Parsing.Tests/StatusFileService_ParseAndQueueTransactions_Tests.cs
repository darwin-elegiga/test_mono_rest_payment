using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using VPay.DocSys.Common;
using VPay.DocSys.Parsing.Common;
using VPay.DocSys.Parsing.Http.Abstractions.PlatformConfiguration.Dto;
using Xunit;

namespace VPay.DocSys.Parsing.Tests
{
    /// <summary>
    /// Tests for <see cref="StatusFileService.ParseAndQueueTransactions"/> method of <see cref="StatusFileService"/> class.
    /// </summary>
    [Collection(nameof(StatusFileService))] // Required to run tests sequentially due to shared file system test files.
    public class StatusFileService_ParseAndQueueTransactions_Tests : BaseStatusFileServiceTests
    {
        [Theory]
        [InlineData(1L)]
        [InlineData(100)]
        public async Task With_StatusFile_NoTransactions_NotRegen_WorksAsExpected(object fieldIdValue)
        {
            var statusFileName = Path.Combine(Path.GetTempPath(), "BABTest.sts");
            SetupInitialMocks(statusFileName);
            SetupStatusFileTracker("PaymentStatusFileId", fieldIdValue);

            var statusFileService = CreateStatusFileService();

            TestFiles.Add(statusFileName);
            File.WriteAllLines(statusFileName, StatusFileData);

            var statusData = new StatusFile(StatusFileData);

            _documentTransactionMessageListBuilderMock.Setup(x => x.BuildMessagesAsync(It.IsAny<DocSysConfiguration>(),
                It.IsAny<StatusFile>(), It.IsAny<string>(), It.IsAny<IList<long>>(), It.IsAny<bool>(), It.IsAny<bool>(),
                It.IsAny<bool>(), It.IsAny<bool>()))
                .ReturnsAsync(BuildSampleDocumentTransactionMessages(statusFileName, statusData).ToList());

            await statusFileService.ParseAndQueueTransactions(statusFileName, new List<long>(), false);

            VerifyRedCardUpdateFileStatusCallTimes(RedCardRepositoryMock, 0);
            VerifyAuxiliaryFileWriterWriteCallTimes(_auxiliaryFileWriterMock, 1);
        }

        [Theory]
        [InlineData(1L)]
        [InlineData(100)]
        public async Task With_StatusFile_NoTransactions_Regen_WorksAsExpected(object fieldIdValue)
        {
            var statusFileName = Path.Combine(Path.GetTempPath(), "BABTest.sts");
            SetupInitialMocks(statusFileName);
            SetupStatusFileTracker("PaymentStatusFileId", fieldIdValue);

            var statusFileService = CreateStatusFileService();

            TestFiles.Add(statusFileName);
            File.WriteAllLines(statusFileName, StatusFileData);

            var statusData = new StatusFile(StatusFileData);

            _documentTransactionMessageListBuilderMock.Setup(x => x.BuildMessagesAsync(It.IsAny<DocSysConfiguration>(),
                It.IsAny<StatusFile>(), It.IsAny<string>(), It.IsAny<IList<long>>(), It.IsAny<bool>(), It.IsAny<bool>(),
                It.IsAny<bool>(), It.IsAny<bool>()))
                .ReturnsAsync(BuildSampleDocumentTransactionMessages(statusFileName, statusData).ToList());

            await statusFileService.ParseAndQueueTransactions(statusFileName, new List<long>(), true);

            VerifyRedCardUpdateFileStatusCallTimes(RedCardRepositoryMock, 0);
            VerifyAuxiliaryFileWriterWriteCallTimes(_auxiliaryFileWriterMock, 0);
        }

        [Fact]
        public async Task Without_StatusFile_WithBadTransaction_Regen_ThrowsError()
        {
            var statusFileName = Path.Combine(Path.GetTempPath(), "BABTest.sts");
            SetupInitialMocks(statusFileName);

            var statusFileService = CreateStatusFileService();

            TestFiles.Add(statusFileName);
            File.WriteAllLines(statusFileName, StatusFileData);

            var statusData = new StatusFile(StatusFileData);

            _statusFileTrackerMock.Setup(x => x.GetStatusFileNameFromTransaction(It.IsAny<long>()))
                .Returns((string)null);

            var transactionMessages = BuildSampleDocumentTransactionMessages(statusFileName, statusData).ToList();
            var transactions = transactionMessages.Select(x => long.Parse(x.TransactionId)).ToList();

            _documentTransactionMessageListBuilderMock.Setup(x => x.BuildMessagesAsync(It.IsAny<DocSysConfiguration>(),
                It.IsAny<StatusFile>(), It.IsAny<string>(), It.IsAny<IList<long>>(), It.IsAny<bool>(), It.IsAny<bool>(),
                It.IsAny<bool>(), It.IsAny<bool>()))
                .ReturnsAsync(transactionMessages);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => statusFileService.ParseAndQueueTransactions(string.Empty, transactions, true));

            VerifyRedCardUpdateFileStatusCallTimes(RedCardRepositoryMock, 0);
            VerifyAuxiliaryFileWriterWriteCallTimes(_auxiliaryFileWriterMock, 0);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task With_StatusFile_NoTransaction_NoData_ReturnsWithNoError(bool isRegen)
        {
            var statusFileName = Path.Combine(Path.GetTempPath(), "BABTest.sts");
            SetupInitialMocks(statusFileName);

            var statusFileService = CreateStatusFileService();

            TestFiles.Add(statusFileName);
            File.WriteAllLines(statusFileName, StatusFileData);

            var statusData = new StatusFile(StatusFileData);
            statusData.Records.Clear();

            File.WriteAllText(statusFileName, statusData.ToString());

            _statusFileTrackerMock.Setup(x => x.GetStatusFileNameFromTransaction(It.IsAny<long>()))
                .Returns((string)null);

            var transactionMessages = BuildSampleDocumentTransactionMessages(statusFileName, statusData).ToList();

            _documentTransactionMessageListBuilderMock.Setup(x => x.BuildMessagesAsync(It.IsAny<DocSysConfiguration>(),
                It.IsAny<StatusFile>(), It.IsAny<string>(), It.IsAny<IList<long>>(), It.IsAny<bool>(), It.IsAny<bool>(),
                It.IsAny<bool>(), It.IsAny<bool>()))
                .ReturnsAsync(transactionMessages);

            await statusFileService.ParseAndQueueTransactions(statusFileName, new List<long>(), isRegen);

            VerifyRedCardUpdateFileStatusCallTimes(RedCardRepositoryMock, 0);
            VerifyAuxiliaryFileWriterWriteCallTimes(_auxiliaryFileWriterMock, 0);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Without_StatusFile_WithoutTransaction_RunsWithoutError(bool isRegen)
        {
            var statusFileName = Path.Combine(Path.GetTempPath(), "BABTest.sts");
            SetupInitialMocks(statusFileName);

            var statusFileService = CreateStatusFileService();

            TestFiles.Add(statusFileName);
            File.WriteAllLines(statusFileName, StatusFileData);

            var statusData = new StatusFile(StatusFileData);

            _statusFileTrackerMock.Setup(x => x.GetStatusFileNameFromTransaction(It.IsAny<long>()))
                .Returns((string)null);

            var transactionMessages = BuildSampleDocumentTransactionMessages(statusFileName, statusData).ToList();

            _documentTransactionMessageListBuilderMock.Setup(x => x.BuildMessagesAsync(It.IsAny<DocSysConfiguration>(),
                It.IsAny<StatusFile>(), It.IsAny<string>(), It.IsAny<IList<long>>(), It.IsAny<bool>(), It.IsAny<bool>(),
                It.IsAny<bool>(), It.IsAny<bool>()))
                .ReturnsAsync(transactionMessages);

            await statusFileService.ParseAndQueueTransactions(string.Empty, new List<long>(), isRegen);

            VerifyRedCardUpdateFileStatusCallTimes(RedCardRepositoryMock, 0);
            VerifyAuxiliaryFileWriterWriteCallTimes(_auxiliaryFileWriterMock, 0);
        }


        [Theory]
        [InlineData(1L)]
        [InlineData(100)]
        public async Task Without_StatusFile_WithTransactions_Regen_WorksAsExpected(object fieldIdValue)
        {
            var statusFileName = Path.Combine(Path.GetTempPath(), "BABTest.sts");
            SetupInitialMocks(statusFileName);
            SetupStatusFileTracker("PaymentStatusFileId", fieldIdValue);

            var statusFileService = CreateStatusFileService();

            TestFiles.Add(statusFileName);
            File.WriteAllLines(statusFileName, StatusFileData);

            var statusData = new StatusFile(StatusFileData);

            _statusFileTrackerMock.Setup(x => x.GetStatusFileNameFromTransaction(It.IsAny<long>()))
                .Returns(statusFileName);

            var transactionMessages = BuildSampleDocumentTransactionMessages(statusFileName, statusData).ToList();
            var transactions = transactionMessages.Select(x => long.Parse(x.TransactionId)).ToList();

            _documentTransactionMessageListBuilderMock.Setup(x => x.BuildMessagesAsync(It.IsAny<DocSysConfiguration>(),
                It.IsAny<StatusFile>(), It.IsAny<string>(), It.IsAny<IList<long>>(), It.IsAny<bool>(), It.IsAny<bool>(),
                It.IsAny<bool>(), It.IsAny<bool>()))
                .ReturnsAsync(transactionMessages);

            await statusFileService.ParseAndQueueTransactions(string.Empty, transactions, true);

            VerifyRedCardUpdateFileStatusCallTimes(RedCardRepositoryMock, 0);
            VerifyAuxiliaryFileWriterWriteCallTimes(_auxiliaryFileWriterMock, 0);
        }

        [Fact]
        public async Task With_PE_Origin_WorksAsExpected()
        {
            var statusFileName = Path.Combine(Path.GetTempPath(), "BPETest.sts");
            SetupInitialMocks(statusFileName);
            _paymentEngineConfirmationFileServiceMock
                .Setup(x => x.ProcessFile(It.IsAny<PaymentEngineConfirmationMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var statusFileService = CreateStatusFileService();

            TestFiles.Add(statusFileName);
            File.WriteAllLines(statusFileName, PEStatusFileData);

            var statusData = new StatusFile(PEStatusFileData);

            _statusFileTrackerMock.Setup(x => x.GetStatusFileNameFromTransaction(It.IsAny<long>()))
                .Returns(statusFileName);

            var transactionMessages = BuildSampleDocumentTransactionMessages(statusFileName, statusData).ToList();
            var transactions = transactionMessages.Select(x => long.Parse(x.TransactionId)).ToList();

            await statusFileService.ParseAndQueueTransactions(string.Empty, transactions, true);

            _paymentEngineConfirmationFileServiceMock.Verify(x => x.ProcessFile(It.IsAny<PaymentEngineConfirmationMessage>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ForStpPayments_BulkInsertsStpPayments()
        {
            var statusFileName = Path.Combine(Path.GetTempPath(), "STPTest.sts");
            SetupInitialMocks(statusFileName);

            var statusFileService = CreateStatusFileService();

            TestFiles.Add(statusFileName);
            File.WriteAllLines(statusFileName, StpStatusFileData);

            var statusData = new StatusFile(StpStatusFileData);
            foreach (var record in statusData.Records)
            {
                record.NETWORKTYPE = "STP";
                record.NETWORKNAME = "MY_NETWORK";
                record.NETWORKENROLLMENTTYPE = "B2B";
            }

            _statusFileTrackerMock
                .Setup(x => x.BulkInsertStpPayments(It.IsAny<IEnumerable<long>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()));

            _statusFileTrackerMock.Setup(x => x.GetStatusFileNameFromTransaction(It.IsAny<long>()))
                .Returns(statusFileName);

            var transactionMessages = BuildSampleStpDocumentTransactionMessages(statusFileName, statusData).ToList();
            var transactions = transactionMessages.Select(x => long.Parse(x.TransactionId)).ToList();

            _documentTransactionMessageListBuilderMock.Setup(x => x.BuildMessagesAsync(It.IsAny<DocSysConfiguration>(),
                It.IsAny<StatusFile>(), It.IsAny<string>(), It.IsAny<IList<long>>(), It.IsAny<bool>(), It.IsAny<bool>(),
                It.IsAny<bool>(), It.IsAny<bool>()))
                .ReturnsAsync(transactionMessages);

            await statusFileService.ParseAndQueueTransactions(string.Empty, transactions, false);

            // Should call BulkInsertStpPayments once for B2B payments and once for C2B payments, if applicable.
            // If only B2B payments are being inserted as new payments in database, this count should be 1.
            // If both B2B and C2B payments are being inserted as new payments in database, this count should be 2.
            // As of the current implementation, only B2B payments are inserted as new payments in database, so we expect this to be called once.
            _statusFileTrackerMock.Verify(x => x.BulkInsertStpPayments(It.IsAny<IEnumerable<long>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()),
                Times.Once());
        }

        [Fact]
        public async Task With_PaymentEngineConfirmationProcessing_DoesNotProcessFile_WhenGenerateDocumentStatusIsFalse()
        {
            // Arrange
            var statusFileName = Path.Combine(Path.GetTempPath(), "BABTest.sts");
            SetupInitialMocks(statusFileName);

            _paymentEngineConfirmationFileServiceMock
            .Setup(x => x.ProcessFile(It.IsAny<PaymentEngineConfirmationMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

            _platformConfigurationApiMock
                .Setup(x => x.GetGenerateDocumentStatus(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GenerateDocumentStatusDto
                {
                    OriginCode = "PE",
                    ClientCode = "Client123",
                    BillingEntityCode = "BillingEntity123",
                    IsGenerateDocumentStatus = "true"
                });

            var statusFileService = CreateStatusFileService();

            TestFiles.Add(statusFileName);
            File.WriteAllLines(statusFileName, PEStatusFileData);

            var statusData = new StatusFile(PEStatusFileData);       

            _statusFileTrackerMock.Setup(x => x.GetStatusFileNameFromTransaction(It.IsAny<long>()))
                .Returns(statusFileName);

            var transactionMessages = BuildSampleDocumentTransactionMessages(statusFileName, statusData).ToList();
            var transactions = transactionMessages.Select(x => long.Parse(x.TransactionId)).ToList();

            _documentTransactionMessageListBuilderMock.Setup(x => x.BuildMessagesAsync(It.IsAny<DocSysConfiguration>(),
             It.IsAny<StatusFile>(), It.IsAny<string>(), It.IsAny<IList<long>>(), It.IsAny<bool>(), It.IsAny<bool>(),
             It.IsAny<bool>(), It.IsAny<bool>()))
             .ReturnsAsync(transactionMessages);

            // Act
            await statusFileService.ParseAndQueueTransactions(string.Empty, transactions, false);

            //Assert
            _platformConfigurationApiMock.Verify(
                 x => x.GetGenerateDocumentStatus(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()),
                 Times.Once);
        }

        [Fact]
        public async Task With_PaymentEngineConfirmationProcessing_ProcessesFile_WhenGenerateDocumentStatusIsTrue()
        {
            // Arrange
            var statusFileName = Path.Combine(Path.GetTempPath(), "BABTest.sts");
            SetupInitialMocks(statusFileName);

            _paymentEngineConfirmationFileServiceMock
                .Setup(x => x.ProcessFile(It.IsAny<PaymentEngineConfirmationMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _platformConfigurationApiMock
                .Setup(x => x.GetGenerateDocumentStatus(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GenerateDocumentStatusDto
                {
                    OriginCode = "PE",
                    ClientCode = "Client123",
                    BillingEntityCode = "BillingEntity123",
                    IsGenerateDocumentStatus = "true"
                });

            var statusFileService = CreateStatusFileService();

            TestFiles.Add(statusFileName);
            File.WriteAllLines(statusFileName, PEStatusFileData);

            var statusData = new StatusFile(PEStatusFileData);

            _statusFileTrackerMock.Setup(x => x.GetStatusFileNameFromTransaction(It.IsAny<long>()))
                .Returns(statusFileName);

            var transactionMessages = BuildSampleDocumentTransactionMessages(statusFileName, statusData).ToList();
            var transactions = transactionMessages.Select(x => long.Parse(x.TransactionId)).ToList();

            _documentTransactionMessageListBuilderMock.Setup(x => x.BuildMessagesAsync(It.IsAny<DocSysConfiguration>(),
            It.IsAny<StatusFile>(), It.IsAny<string>(), It.IsAny<IList<long>>(), It.IsAny<bool>(), It.IsAny<bool>(),
            It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(transactionMessages);

            // Act
            await statusFileService.ParseAndQueueTransactions(string.Empty, transactions, false);
            // Assert
       
            _platformConfigurationApiMock.Verify(
                x => x.GetGenerateDocumentStatus(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }


        protected static IEnumerable<DocumentTransactionMessage> BuildSampleStpDocumentTransactionMessages(string stsFilename, StatusFile statusFile)
        {
            if (statusFile?.Records != null)
            {
                foreach (var record in statusFile.Records)
                {
                    if (record.STATUS == "DENIED")
                    {
                        continue;
                    }
                    record.NETWORKNAME = "MY_NETWORK";
                    record.NETWORKENROLLMENTTYPE = "B2B";
                    record.NETWORKTYPE = "STP";

                    yield return new DocumentTransactionMessage
                    {
                        GenerateEOB = false,
                        StatusFilePath = stsFilename,
                        RegeneratePackage = false,
                        InputFile = statusFile.Header.InputFile,
                        TransactionId = record.TXID,
                        TPA = record.TPA,
                        IsRedCard = false,
                        GUID = record.GUID,
                        BillingEntity = record.BILENT,
                        CDate = statusFile.Header.CDate,
                        StatusRecord = record,
                        ConfigData = ""
                    };
                }
            }
        }

        protected static IEnumerable<DocumentTransactionMessage> BuildSampleDocumentTransactionMessages(string stsFilename, StatusFile statusFile)
        {
            if (statusFile?.Records != null)
            {
                foreach (var record in statusFile.Records)
                {
                    if (record.STATUS == "DENIED")
                    {
                        continue;
                    }

                    yield return new DocumentTransactionMessage
                    {
                        GenerateEOB = false,

                        StatusFilePath = stsFilename,
                        RegeneratePackage = false,
                        InputFile = statusFile.Header.InputFile,
                        TransactionId = record.TXID,
                        TPA = record.TPA,
                        IsRedCard = false,
                        GUID = record.GUID,
                        BillingEntity = record.BILENT,
                        CDate = statusFile.Header.CDate,
                        StatusRecord = record,
                        ConfigData = ""
                    };
                }
            }
        }
    }
}
