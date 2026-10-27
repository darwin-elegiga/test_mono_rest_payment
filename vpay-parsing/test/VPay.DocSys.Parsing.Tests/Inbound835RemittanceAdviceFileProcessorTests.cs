using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using VPay.DocSys.Data;
using VPay.DocSys.Parsing.Common;
using VPay.DocSys.Parsing.Common.Inbound835Files;
using VPay.DocSys.Parsing.Http.Abstractions.PlatformConfiguration;
using VPay.DocSys.Parsing.Http.Abstractions.PlatformConfiguration.Dto;
using VPay.DocSys.Parsing.Tests.Helpers;
using VPay.DocSys.Storage;
using Xunit;

namespace VPay.DocSys.Parsing.Tests
{
    public class Inbound835RemittanceAdviceFileProcessorTests
    {
        private static Pagination<EdiDeliveryClientBoardingConfigDto> CreateEdiDeliveryConfigDto(params (string OriginCode, string ClientCode, string EntityCode, EdiDeliveryDestination Destination, EdiDeliveryFileExtension Extension)[] codes)
        {
            var pagination = new Pagination<EdiDeliveryClientBoardingConfigDto>();
            foreach (var code in codes)
            {
                pagination.Items.Add(new EdiDeliveryClientBoardingConfigDto
                {
                    BeginDate = DateTime.Today.AddYears(-5),
                    EndDate = DateTime.Today.AddYears(5),
                    Status = ClientBoardingConfigDtoBase.ACTIVE,
                    OriginCode = code.OriginCode,
                    ClientCode = code.ClientCode,
                    BillingEntityCode = code.EntityCode,
                    Destination = code.Destination,
                    FileExtension = code.Extension
                });
            }
            return pagination;
        }

        private readonly MockFileSystem _mockFileSystem = new MockFileSystem();
        private readonly Mock<ISftpService> _mockSftpService = new Mock<ISftpService>();
        private readonly Mock<IEdi835Repository> _mockEdi835Repository = new Mock<IEdi835Repository>();
        private readonly Mock<IInbound835FileParser> _mockInbound835FileParser = new Mock<IInbound835FileParser>();
        private readonly Mock<IPlatformConfigurationApi> _mockPlatformConfigurationApi = new();
        private readonly Inbound835FileServiceSettings _dummyInbound835FileServiceSettings = new Inbound835FileServiceSettings { };
        private readonly Mock<IStorageManager> _mockStorageManager = new Mock<IStorageManager>();
        private readonly Mock<ILogger<Inbound835RemittanceAdviceFileProcessor>> _mockLogger = new Mock<ILogger<Inbound835RemittanceAdviceFileProcessor>>();
        private const string _dummyRemittanceAdviceFilePath = "./remittanceTestData/test.835";

        private IServiceProvider _serviceProvider => new ServiceCollection()
            .AddTransient<IFileSystem>(s => _mockFileSystem)
            .AddTransient(s => _mockSftpService.Object)
            .AddTransient(s => _mockEdi835Repository.Object)
            .AddTransient(s => _mockInbound835FileParser.Object)
            .AddTransient(s => _mockPlatformConfigurationApi.Object)
            .AddTransient(s => _dummyInbound835FileServiceSettings)
            .AddTransient(s => _mockStorageManager.Object)
            .AddTransient(s => _mockLogger.Object)
            .AddTransient<Inbound835RemittanceAdviceFileProcessor>()
            .BuildServiceProvider();

        public static IEnumerable<object[]> ProcessFile_Data = new List<object[]>
        {
            // default
            new object[]
            {
                new Inbound835FileMessage { FilePath = "test" },
                new RemittanceAdviceFile { FileName = "test.835", FullFileName = _dummyRemittanceAdviceFilePath },
                Inbound835FileType.RemittanceAdvice,
                new Edi835 { StatusCode = Edi835StatusCode.Sent835ToAvaility },
                new List<Edi835Status> { new Edi835Status{ StatusCode = Edi835StatusCode.Received835FromVendor } },
                new List<IArchiveFile> { new ArchiveFile { } },
                new List<string>{ "test.835" },
                123456L,
                new SendToAvailitySettings { Enabled = false, TransferSettings = new SendToAvailityTransferSettings { } },
                CreateEdiDeliveryConfigDto(),
                Times.Never(), Times.Never(), Times.Never(), Times.Never()
            },
            // unique remittance file, disabled delivery (will AddRemittanceAdviceFileToNewDocumentArchive)
            new object[]
            {
                new Inbound835FileMessage { FilePath = "test" },
                new RemittanceAdviceFile { FileName = "test.835", FullFileName = _dummyRemittanceAdviceFilePath },
                Inbound835FileType.RemittanceAdvice,
                new Edi835 { ClientCode = "TPA", BillingEntityCode = "BE", StatusCode = Edi835StatusCode.Sent835ToAvaility },
                new List<Edi835Status> { new Edi835Status{ StatusCode = Edi835StatusCode.Received835FromVendor } },
                new List<IArchiveFile> { new ArchiveFile { } },
                new List<string>{ },
                123456L,
                new SendToAvailitySettings { Enabled = false, TransferSettings = new SendToAvailityTransferSettings { } },
                CreateEdiDeliveryConfigDto(("DEFAULT","","",EdiDeliveryDestination.None,EdiDeliveryFileExtension.None)),
                Times.Once(), Times.Once(), Times.Never(), Times.Never()
            },
            // edi835 status less than 800 (will Add835Status for Received835FromVendor
            new object[]
            {
                new Inbound835FileMessage { FilePath = "test" },
                new RemittanceAdviceFile { FileName = "test.835", FullFileName = _dummyRemittanceAdviceFilePath },
                Inbound835FileType.RemittanceAdvice,
                new Edi835 { ClientCode = "TPA", BillingEntityCode = "BE", StatusCode = Edi835StatusCode.Initiated },
                new List<Edi835Status> { new Edi835Status{ StatusCode = Edi835StatusCode.Initiated } },
                new List<IArchiveFile> { new ArchiveFile { } },
                new List<string>{ "test.835" },
                123456L,
                new SendToAvailitySettings { Enabled = false, TransferSettings = new SendToAvailityTransferSettings { } },
                CreateEdiDeliveryConfigDto(("OC","TPA","BE",EdiDeliveryDestination.Availity,EdiDeliveryFileExtension.Edi835)),
                Times.Never(), Times.Exactly(2), Times.Never(), Times.Never()
            },
            // edi835 status less than 850 (will Add835Status for ReceiveBrnFromVendor
            new object[]
            {
                new Inbound835FileMessage { FilePath = "test.brn" },
                new RemittanceAdviceFile { FileName = "test.brn", FullFileName = _dummyRemittanceAdviceFilePath },
                Inbound835FileType.BlueRemittanceNotification,
                new Edi835 { StatusCode = Edi835StatusCode.Initiated, ClientCode = "TPA", BillingEntityCode = "BE" },
                new List<Edi835Status> { new Edi835Status{ StatusCode = Edi835StatusCode.Initiated } },
                new List<IArchiveFile> { new ArchiveFile { } },
                new List<string>{ "test.brn" },
                123456L,
                new SendToAvailitySettings { Enabled = false, TransferSettings = new SendToAvailityTransferSettings { } },
                CreateEdiDeliveryConfigDto(("OC","TPA","BE",EdiDeliveryDestination.Availity,EdiDeliveryFileExtension.EdiBRN)),
                Times.Never(), Times.Exactly(2), Times.Never(), Times.Never()
            },
            // edi835 status less than 900 and matching client/be codes (will Add835Status for Sent835ToAvailability)
            new object[]
            {
                new Inbound835FileMessage { FilePath = "test" },
                new RemittanceAdviceFile { FileName = "test.835", FullFileName = _dummyRemittanceAdviceFilePath },
                Inbound835FileType.RemittanceAdvice,
                new Edi835 { ClientCode = "TPA", BillingEntityCode = "BE", StatusCode = Edi835StatusCode.Received835FromVendor },
                new List<Edi835Status> { new Edi835Status{ StatusCode = Edi835StatusCode.Received835FromVendor } },
                new List<IArchiveFile> { new ArchiveFile { } },
                new List<string>{ "test.835" },
                123456L,
                new SendToAvailitySettings { Enabled = false, TransferSettings = new SendToAvailityTransferSettings { } },
                CreateEdiDeliveryConfigDto(("OC","TPA","BE",EdiDeliveryDestination.Availity,EdiDeliveryFileExtension.Edi835)),
                Times.Never(), Times.Once(), Times.Never(), Times.Never()
            },
            // edi835 status less than 950 (will Add835Status for ReceiveBrnFromVendor
            new object[]
            {
                new Inbound835FileMessage { FilePath = "test.brn" },
                new RemittanceAdviceFile { FileName = "test.brn", FullFileName = _dummyRemittanceAdviceFilePath },
                Inbound835FileType.BlueRemittanceNotification,
                new Edi835 { StatusCode = Edi835StatusCode.Initiated, ClientCode = "TPA", BillingEntityCode = "BE" },
                new List<Edi835Status> { new Edi835Status{ StatusCode = Edi835StatusCode.ReceivedBRNFromVendor } },
                new List<IArchiveFile> { new ArchiveFile { } },
                new List<string>{ "test.brn" },
                123456L,
                new SendToAvailitySettings { Enabled = false, TransferSettings = new SendToAvailityTransferSettings { } },
                CreateEdiDeliveryConfigDto(("OC","TPA","BE",EdiDeliveryDestination.Availity,EdiDeliveryFileExtension.EdiBRN)),
                Times.Never(), Times.Once(), Times.Never(), Times.Never()
            },
            // matching client/be codes and enabled send via password (will TransferFile with password)
            new object[]
            {
                new Inbound835FileMessage { FilePath = "test" },
                new RemittanceAdviceFile { FileName = "test.835", FullFileName = _dummyRemittanceAdviceFilePath },
                Inbound835FileType.RemittanceAdvice,
                new Edi835 { ClientCode = "TPA", BillingEntityCode = "BE", StatusCode = Edi835StatusCode.Received835FromVendor },
                new List<Edi835Status> { new Edi835Status{ StatusCode = Edi835StatusCode.Received835FromVendor } },
                new List<IArchiveFile> { new ArchiveFile { } },
                new List<string>{ "test.835" },
                123456L,
                new SendToAvailitySettings { Enabled = true, TransferSettings = new SendToAvailityTransferSettings { UseRsaKey = false } },
                CreateEdiDeliveryConfigDto(("OC","TPA","BE",EdiDeliveryDestination.Availity,EdiDeliveryFileExtension.Edi835)),
                Times.Never(), Times.Once(), Times.Once(), Times.Never()
            },
            // matching client/be codes and enabled send via rsa (will TransferFile without password)
            new object[]
            {
                new Inbound835FileMessage { FilePath = "test" },
                new RemittanceAdviceFile { FileName = "test.835", FullFileName = _dummyRemittanceAdviceFilePath },
                Inbound835FileType.RemittanceAdvice,
                new Edi835 { ClientCode = "TPA", BillingEntityCode = "BE", StatusCode = Edi835StatusCode.Received835FromVendor },
                new List<Edi835Status> { new Edi835Status{ StatusCode = Edi835StatusCode.Received835FromVendor } },
                new List<IArchiveFile> { new ArchiveFile { } },
                new List<string>{ "test.835" },
                123456L,
                new SendToAvailitySettings { Enabled = true, TransferSettings = new SendToAvailityTransferSettings { UseRsaKey = true } },
                CreateEdiDeliveryConfigDto(("OC","TPA","BE",EdiDeliveryDestination.Availity,EdiDeliveryFileExtension.Edi835)),
                Times.Never(), Times.Once(), Times.Never(), Times.Once()
            },
        };

        public static IEnumerable<object[]> ProcessFile_DataForParseRemittanceAdviceFileErrors = new List<object[]>
        {
            new object[] { new Inbound835FileMessage { FilePath = "test" } }
        };

        public static IEnumerable<object[]> ProcessFile_DataForGetMostRecent835Errors = new List<object[]>
        {
            new object[] { new Inbound835FileMessage { FilePath = "test" }, new RemittanceAdviceFile { } }
        };

        public static IEnumerable<object[]> ProcessFile_DataForGetArchivesForTransactionIdErrors = new List<object[]>
        {
            new object[] { new Inbound835FileMessage { FilePath = "test" }, new RemittanceAdviceFile { }, new Edi835 { } }
        };

        public static IEnumerable<object[]> ProcessFile_DataForCreateArchiveErrors = new List<object[]>
        {
            new object[] { new Inbound835FileMessage { FilePath = "test" }, new RemittanceAdviceFile { FullFileName = "test" }, new Edi835 { }, new List<IArchiveFile> { new ArchiveFile { } }, new List<string> { } }
        };

        public static IEnumerable<object[]> ProcessFile_DataForAdd835StatusErrors = new List<object[]>
        {
            // update on line 168
            new object[]
            {
                new Inbound835FileMessage { FilePath = "test" },
                new RemittanceAdviceFile { FileName = "test.835" },
                new Edi835 { },
                new List<Edi835Status> { new Edi835Status { StatusCode = Edi835StatusCode.VendorAcknowledged } },
                new List<IArchiveFile> { new ArchiveFile { } },
                new List<string> { "test.835" },
                new SendToAvailitySettings { Enabled = false },
                CreateEdiDeliveryConfigDto(("DEFAULT","","",EdiDeliveryDestination.None,EdiDeliveryFileExtension.None)),
                "expected error on Add835Status 800"
            },
            // update on line 190
            new object[]
            {
                new Inbound835FileMessage { FilePath = "test" },
                new RemittanceAdviceFile { FileName = "test.835" },
                new Edi835 { StatusCode = Edi835StatusCode.Received835FromVendor, ClientCode = "TPA", BillingEntityCode = "BE" },
                new List<Edi835Status> { new Edi835Status { StatusCode = Edi835StatusCode.Received835FromVendor } },
                new List<IArchiveFile> { new ArchiveFile { } },
                new List<string> { "test.835" },
                new SendToAvailitySettings { Enabled = false },
                CreateEdiDeliveryConfigDto(("OC","TPA","BE",EdiDeliveryDestination.Availity,EdiDeliveryFileExtension.Edi835)),
                "expected error on Add835Status 900"
            },
        };

        public static IEnumerable<object[]> ProcessFile_DataForTransferFileErrors = new List<object[]>
        {
            // line 214
            new object[]
            {
                new Inbound835FileMessage { FilePath = "test" },
                new RemittanceAdviceFile { FileName = "test.835" },
                new Edi835 { StatusCode = Edi835StatusCode.Sent835ToAvaility, ClientCode = "TPA", BillingEntityCode = "BE" },
                new List<Edi835Status> {
                    new Edi835Status { StatusCode = Edi835StatusCode.Received835FromVendor },
                    new Edi835Status { StatusCode = Edi835StatusCode.Sent835ToAvaility } },
                new List<IArchiveFile> { new ArchiveFile { } },
                new List<string> { "test.835" },
                new SendToAvailitySettings { Enabled = true, TransferSettings = new SendToAvailityTransferSettings { UseRsaKey = true } },
                CreateEdiDeliveryConfigDto(("OC","TPA","BE",EdiDeliveryDestination.Availity,EdiDeliveryFileExtension.Edi835)),
                "expected error for rsa path"
            },
            // line 223
            new object[]
            {
                new Inbound835FileMessage { FilePath = "test" },
                new RemittanceAdviceFile { FileName = "test.835" },
                new Edi835 { StatusCode = Edi835StatusCode.Sent835ToAvaility, ClientCode = "TPA", BillingEntityCode = "BE" },
                new List<Edi835Status> {
                    new Edi835Status { StatusCode = Edi835StatusCode.Received835FromVendor },
                    new Edi835Status { StatusCode = Edi835StatusCode.Sent835ToAvaility } },
                new List<IArchiveFile> { new ArchiveFile { } },
                new List<string> { "test.835" },
                new SendToAvailitySettings { Enabled = true, TransferSettings = new SendToAvailityTransferSettings { UseRsaKey = false } },
                CreateEdiDeliveryConfigDto(("OC","TPA","BE",EdiDeliveryDestination.Availity,EdiDeliveryFileExtension.Edi835)),
                "expected error for password path"
            },
        };

        public static IEnumerable<object[]> SendBrnToAvaility_Data = new List<object[]>
        {
            new object[]
            {
                new Edi835 { ClientCode = "TST", BillingEntityCode = "TST0001001", StatusCode = Edi835StatusCode.SentBRNToAvaility },
                new List<Edi835Status> { new Edi835Status{ StatusCode = Edi835StatusCode.ReceivedBRNFromVendor, AssociatedFileName = "GLORP" } },
                new RemittanceAdviceFile { FileName = "test.835" },
                new SendToAvailitySettings
                {
                    Enabled = true,
                    TransferSettings = new SendToAvailityTransferSettings
                    {
                        UseRsaKey = true
                    }
                },
                CreateEdiDeliveryConfigDto(("OC","TST","TST0001001",EdiDeliveryDestination.Availity,EdiDeliveryFileExtension.EdiBRN)),
            }
        };

        [Theory]
        [MemberData(nameof(ProcessFile_Data))]
        public async Task ProcessFile_ShouldPass_WhenDefault(
            Inbound835FileMessage message,
            RemittanceAdviceFile expectedFile,
            Inbound835FileType expectedFileType,
            Edi835 expected835,
            List<Edi835Status> expected835Status,
            List<IArchiveFile> expectedArchives,
            List<string> expectedArchiveDetailNames,
            long expectedArchiveId,
            SendToAvailitySettings expectedSettings,
            Pagination<EdiDeliveryClientBoardingConfigDto> expectedClientConfig,
            Times expectedCreateArchive, Times expectedAdd835Status, Times expectedTransferFileWithPassword, Times expectedTransferFileWithRsa)
        {
            // arrange
            _mockInbound835FileParser
                .Setup(m => m.ParseRemittanceAdviceFile(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedFile);
            _mockInbound835FileParser
                .Setup(m => m.GetFileType(It.IsAny<string>()))
                .Returns(expectedFileType);
            _mockEdi835Repository
                .Setup(m => m.GetMostRecent835(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected835);
            _mockEdi835Repository
                .Setup(m => m.Get835StatusHistory(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected835Status);
            _mockStorageManager
                .Setup(m => m.GetArchivesForTransactionId(It.IsAny<long>(), It.IsAny<bool>()))
                .ReturnsAsync(expectedArchives);
            _mockStorageManager
                .Setup(m => m.GetArchiveFileDetailNames(It.IsAny<IArchiveFile>()))
                .Returns(expectedArchiveDetailNames);
            _mockStorageManager
                .Setup(m => m.CreateArchive(It.IsAny<IArchiveFile>(), It.IsAny<List<IGeneratedFile>>(), It.IsAny<bool>(), It.IsAny<DateTime?>()))
                .ReturnsAsync(expectedArchiveId);
            _mockPlatformConfigurationApi
                .Setup(m => m.GetEdiDeliveryByClient(It.IsAny<string>(), It.IsAny<string>(), default))
                .ReturnsAsync(expectedClientConfig);
            _dummyInbound835FileServiceSettings
                .RemittanceAdviceFile.SendToAvaility = expectedSettings;
            _mockFileSystem.AddFile(
                expectedFile.FullFileName,
                new MockFileData("FAKE FILE CONTENT"));
            var sut = _serviceProvider.GetRequiredService<Inbound835RemittanceAdviceFileProcessor>();

            // act
            await sut.ProcessFile(message);

            // assert
            _mockLogger.VerifyLog(LogLevel.Information, Times.AtLeastOnce());
            _mockLogger.VerifyLog(LogLevel.Error, Times.Never());
            _mockStorageManager
                .Verify(m => m.CreateArchive(It.IsAny<IArchiveFile>(), It.IsAny<List<IGeneratedFile>>(), It.IsAny<bool>(), It.IsAny<DateTime?>()),
                        expectedCreateArchive);
            _mockEdi835Repository
                .Verify(m => m.Add835Status(It.IsAny<long>(), It.IsAny<Edi835StatusCode>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                    expectedAdd835Status);
            _mockSftpService
                .Verify(m => m.TransferFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<short?>(), It.IsAny<CancellationToken>()),
                    expectedTransferFileWithPassword);
            _mockSftpService
                .Verify(m => m.TransferFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<short?>(), It.IsAny<CancellationToken>()),
                    expectedTransferFileWithRsa);
        }

        [Theory]
        [MemberData(nameof(ProcessFile_DataForParseRemittanceAdviceFileErrors))]
        public async Task ProcessFile_ShouldThrow_WhenInbound835FileParserThrows(Inbound835FileMessage message)
        {
            // arrange
            var expectedError = "expected error";
            _mockInbound835FileParser
                .Setup(m => m.ParseRemittanceAdviceFile(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new NotImplementedException(expectedError));
            var sut = _serviceProvider.GetRequiredService<Inbound835RemittanceAdviceFileProcessor>();

            // act
            Func<Task> act = async () => await sut.ProcessFile(message);

            (await act.Should().ThrowAsync<NotImplementedException>()).WithMessage(expectedError);
            _mockLogger
                .VerifyLog(LogLevel.Information, Times.Once())
                .VerifyLog(LogLevel.Error, Times.Once());
        }

        [Theory]
        [MemberData(nameof(ProcessFile_DataForGetMostRecent835Errors))]
        public async Task ProcessFile_ShouldThrow_WhenEdi835RepositoryThrows_FromGetMostRecent835(Inbound835FileMessage message, RemittanceAdviceFile expectedFile)
        {
            // arrange
            var expectedError = "expected error";
            _mockInbound835FileParser
                .Setup(m => m.ParseRemittanceAdviceFile(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedFile);
            _mockEdi835Repository
                .Setup(m => m.GetMostRecent835(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new NotImplementedException(expectedError));
            var sut = _serviceProvider.GetRequiredService<Inbound835RemittanceAdviceFileProcessor>();

            // act
            Func<Task> act = async () => await sut.ProcessFile(message);

            (await act.Should().ThrowAsync<NotImplementedException>()).WithMessage(expectedError);
            _mockLogger
                .VerifyLog(LogLevel.Information, Times.Exactly(3))
                .VerifyLog(LogLevel.Error, Times.Once());
        }

        [Theory]
        [MemberData(nameof(ProcessFile_DataForGetMostRecent835Errors))]
        public async Task ProcessFile_ShouldThrow_WhenEdi835RepositoryReturnsNull(Inbound835FileMessage message, RemittanceAdviceFile expectedFile)
        {
            // arrange
            _mockInbound835FileParser
                .Setup(m => m.ParseRemittanceAdviceFile(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedFile);
            _mockEdi835Repository
                .Setup(m => m.GetMostRecent835(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(default(Edi835));
            var sut = _serviceProvider.GetRequiredService<Inbound835RemittanceAdviceFileProcessor>();

            // act
            Func<Task> act = async () => await sut.ProcessFile(message);

            (await act.Should().ThrowAsync<Exception>()).WithMessage("No 835 request found for transaction*");
            _mockLogger
                .VerifyLog(LogLevel.Information, Times.Exactly(4))
                .VerifyLog(LogLevel.Error, Times.Once());
        }

        [Theory]
        [MemberData(nameof(ProcessFile_DataForGetArchivesForTransactionIdErrors))]
        public async Task ProcessFile_ShouldThrow_WhenStorageManagerThrows(Inbound835FileMessage message, RemittanceAdviceFile expectedFile, Edi835 expected835)
        {
            // arrange
            var expectedError = "expected error";
            _mockInbound835FileParser
                .Setup(m => m.ParseRemittanceAdviceFile(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedFile);
            _mockEdi835Repository
                .Setup(m => m.GetMostRecent835(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected835);
            _mockStorageManager
                .Setup(m => m.GetArchivesForTransactionId(It.IsAny<long>(), It.IsAny<bool>()))
                .ThrowsAsync(new NotImplementedException(expectedError));
            var sut = _serviceProvider.GetRequiredService<Inbound835RemittanceAdviceFileProcessor>();

            // act
            Func<Task> act = async () => await sut.ProcessFile(message);

            (await act.Should().ThrowAsync<NotImplementedException>()).WithMessage(expectedError);
            _mockLogger
                .VerifyLog(LogLevel.Information, Times.Exactly(6))
                .VerifyLog(LogLevel.Error, Times.Once());
        }

        [Theory]
        [MemberData(nameof(ProcessFile_DataForGetArchivesForTransactionIdErrors))]
        public async Task ProcessFile_ShouldThrow_WhenStorageManagerReturnsNull(Inbound835FileMessage message, RemittanceAdviceFile expectedFile, Edi835 expected835)
        {
            // arrange
            _mockInbound835FileParser
                .Setup(m => m.ParseRemittanceAdviceFile(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedFile);
            _mockEdi835Repository
                .Setup(m => m.GetMostRecent835(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected835);
            _mockStorageManager
                .Setup(m => m.GetArchivesForTransactionId(It.IsAny<long>(), It.IsAny<bool>()))
                .ReturnsAsync(new List<IArchiveFile> { });
            var sut = _serviceProvider.GetRequiredService<Inbound835RemittanceAdviceFileProcessor>();

            // act
            Func<Task> act = async () => await sut.ProcessFile(message);

            (await act.Should().ThrowAsync<Exception>()).WithMessage("No archive found for transaction id: *");
            _mockLogger
                .VerifyLog(LogLevel.Information, Times.Exactly(7))
                .VerifyLog(LogLevel.Error, Times.Once());
        }

        [Theory]
        [MemberData(nameof(ProcessFile_DataForCreateArchiveErrors))]
        public async Task ProcessFile_ShouldThrow_WhenStorageManagerThrowsFromCreate(Inbound835FileMessage message, RemittanceAdviceFile expectedFile, Edi835 expected835, List<IArchiveFile> expectedArchives, List<string> expectedArchiveDetailNames)
        {
            // arrange
            var expectedError = "expected error";
            _mockInbound835FileParser
                .Setup(m => m.ParseRemittanceAdviceFile(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedFile);
            _mockEdi835Repository
                .Setup(m => m.GetMostRecent835(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected835);
            _mockStorageManager
                .Setup(m => m.GetArchivesForTransactionId(It.IsAny<long>(), It.IsAny<bool>()))
                .ReturnsAsync(expectedArchives);
            _mockStorageManager
                .Setup(m => m.GetArchiveFileDetailNames(It.IsAny<IArchiveFile>()))
                .Returns(expectedArchiveDetailNames);
            _mockStorageManager
                .Setup(m => m.CreateArchive(It.IsAny<IArchiveFile>(), It.IsAny<List<IGeneratedFile>>(), It.IsAny<bool>(), It.IsAny<DateTime?>()))
                .ThrowsAsync(new NotImplementedException(expectedError));
            _mockFileSystem.AddFile(
                expectedFile.FullFileName,
                new MockFileData("FAKE FILE CONTENT"));
            var sut = _serviceProvider.GetRequiredService<Inbound835RemittanceAdviceFileProcessor>();

            // act
            Func<Task> act = async () => await sut.ProcessFile(message);

            (await act.Should().ThrowAsync<NotImplementedException>()).WithMessage(expectedError);
            _mockLogger
                .VerifyLog(LogLevel.Information, Times.Exactly(8))
                .VerifyLog(LogLevel.Error, Times.Once());
        }

        [Theory]
        [MemberData(nameof(ProcessFile_DataForAdd835StatusErrors))]
        public async Task ProcessFile_ShouldThrow_WhenEdi835RepositoryThrows_FromAdd835Status(
            Inbound835FileMessage message,
            RemittanceAdviceFile expectedFile,
            Edi835 expected835,
            List<Edi835Status> expected835Status,
            List<IArchiveFile> expectedArchives,
            List<string> expectedArchiveDetailNames,
            SendToAvailitySettings expectedSettings,
            Pagination<EdiDeliveryClientBoardingConfigDto> expectedClientConfig,
            string expectedError)
        {
            // arrange
            _mockInbound835FileParser
                .Setup(m => m.ParseRemittanceAdviceFile(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedFile);
            _mockEdi835Repository
                .Setup(m => m.GetMostRecent835(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected835);
            _mockEdi835Repository
                .Setup(m => m.Get835StatusHistory(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected835Status);
            _mockStorageManager
                .Setup(m => m.GetArchivesForTransactionId(It.IsAny<long>(), It.IsAny<bool>()))
                .ReturnsAsync(expectedArchives);
            _mockStorageManager
                .Setup(m => m.GetArchiveFileDetailNames(It.IsAny<IArchiveFile>()))
                .Returns(expectedArchiveDetailNames);
            _mockPlatformConfigurationApi
                .Setup(m => m.GetEdiDeliveryByClient(It.IsAny<string>(), It.IsAny<string>(), default))
                .ReturnsAsync(expectedClientConfig);
            _dummyInbound835FileServiceSettings
                .RemittanceAdviceFile.SendToAvaility = expectedSettings;
            _mockEdi835Repository
                .Setup(m => m.Add835Status(It.IsAny<long>(), It.Is<Edi835StatusCode>(e => e == Edi835StatusCode.Received835FromVendor),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new NotImplementedException("expected error on Add835Status 800"));
            _mockEdi835Repository
                .Setup(m => m.Add835Status(It.IsAny<long>(), It.Is<Edi835StatusCode>(e => e == Edi835StatusCode.Sent835ToAvaility),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new NotImplementedException("expected error on Add835Status 900"));
            var sut = _serviceProvider.GetRequiredService<Inbound835RemittanceAdviceFileProcessor>();

            // act
            Func<Task> act = async () => await sut.ProcessFile(message);

            // assert
            (await act.Should().ThrowAsync<NotImplementedException>()).WithMessage(expectedError);
        }

        [Theory]
        [MemberData(nameof(ProcessFile_DataForTransferFileErrors))]
        public async Task ProcessFile_ShouldThrow_WhenSftpServiceThrows(
            Inbound835FileMessage message,
            RemittanceAdviceFile expectedFile,
            Edi835 expected835,
            List<Edi835Status> expected835Status,
            List<IArchiveFile> expectedArchives,
            List<string> expectedArchiveDetailNames,
            SendToAvailitySettings expectedSettings,
            Pagination<EdiDeliveryClientBoardingConfigDto> expectedClientConfig,
            string expectedError)
        {
            // arrange
            _mockInbound835FileParser
                .Setup(m => m.ParseRemittanceAdviceFile(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedFile);
            _mockEdi835Repository
                .Setup(m => m.GetMostRecent835(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected835);
            _mockEdi835Repository
                .Setup(m => m.Get835StatusHistory(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected835Status);
            _mockStorageManager
                .Setup(m => m.GetArchivesForTransactionId(It.IsAny<long>(), It.IsAny<bool>()))
                .ReturnsAsync(expectedArchives);
            _mockStorageManager
                .Setup(m => m.GetArchiveFileDetailNames(It.IsAny<IArchiveFile>()))
                .Returns(expectedArchiveDetailNames);
            _mockPlatformConfigurationApi
                .Setup(m => m.GetEdiDeliveryByClient(It.IsAny<string>(), It.IsAny<string>(), default))
                .ReturnsAsync(expectedClientConfig);
            _dummyInbound835FileServiceSettings
                .RemittanceAdviceFile.SendToAvaility = expectedSettings;
            _mockSftpService
                .Setup(m => m.TransferFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<short?>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new NotImplementedException("expected error for rsa path"));
            _mockSftpService
                .Setup(m => m.TransferFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<short?>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new NotImplementedException("expected error for password path"));
            var sut = _serviceProvider.GetRequiredService<Inbound835RemittanceAdviceFileProcessor>();

            // act
            Func<Task> act = async () => await sut.ProcessFile(message);

            // assert
            (await act.Should().ThrowAsync<NotImplementedException>()).WithMessage(expectedError);
        }

        [Theory]
        [MemberData(nameof(SendBrnToAvaility_Data))]
        public async Task SendBrnToAvaility_Should_Process(Edi835 expected835, List<Edi835Status> expected835Status, RemittanceAdviceFile expectedFile, SendToAvailitySettings expectedSettings, Pagination<EdiDeliveryClientBoardingConfigDto> expectedEdiDeliveryDto)
        {
            _mockEdi835Repository
                .Setup(m => m.GetMostRecent835(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected835);
            _mockEdi835Repository
                .Setup(m => m.Get835StatusHistory(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected835Status);
            _dummyInbound835FileServiceSettings
                .RemittanceAdviceFile.SendToAvaility = expectedSettings;
            _mockPlatformConfigurationApi
                .Setup(m => m.GetEdiDeliveryByClient(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedEdiDeliveryDto);
            _mockInbound835FileParser
                .Setup(m => m.ParseRemittanceAdviceFile(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedFile);

            var sut = _serviceProvider.GetRequiredService<Inbound835RemittanceAdviceFileProcessor>();

            await sut.SendBrnToAvaility(12345, default);

            _mockEdi835Repository
                .Verify(m => m.GetMostRecent835(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockEdi835Repository
                .Verify(m => m.Get835StatusHistory(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockPlatformConfigurationApi
                .Verify(m => m.GetEdiDeliveryByClient(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockInbound835FileParser
                .Verify(m => m.ParseRemittanceAdviceFile(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
