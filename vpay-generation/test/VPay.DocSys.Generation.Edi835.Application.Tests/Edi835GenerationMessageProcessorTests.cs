using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Newtonsoft.Json;
using VPay.DocSys.Common;
using VPay.DocSys.Data;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Common.Edi835;
using VPay.DocSys.Generation.Edi835.Application.Tests.TestModels;
using Xunit;
using DS = VPay.DocSys.Data;

namespace VPay.DocSys.Generation.Edi835.Application.Tests
{
    public class Edi835GenerationMessageProcessorTests
    {
        private MockRepository mockRepository;

        private Edi835ServiceSettings mockEdi835ServiceSettings;
        private MoqFileSystem mockFileSystem;
        private Mock<IEdi835CreationService> mockEdi835CreationService;
        private Mock<IEdi835Repository> mockEdi835Repository;
        private Mock<IFileTransferService> mockFileTransferService;
        private Mock<ILogger<Edi835GenerationMessageProcessor>> mockLogger;

        public Edi835GenerationMessageProcessorTests()
        {
            this.mockRepository = new MockRepository(MockBehavior.Loose);

            this.mockEdi835ServiceSettings = new Edi835ServiceSettings { OutboundTarFolder = "." };
            this.mockFileSystem = new MoqFileSystem(new Dictionary<string, MockFileData>());
            mockFileSystem.AddDirectory(this.mockEdi835ServiceSettings.OutboundTarFolder);

            // _settings.TarFileTransfer.TransferPath
            this.mockEdi835CreationService = this.mockRepository.Create<IEdi835CreationService>();
            this.mockEdi835CreationService
                    .Setup(_ => _.GenerateTarPackage(It.IsAny<DS.Edi835>()))
                    .ReturnsAsync(FakeTarPackage())
                     ;

            this.mockEdi835Repository = this.mockRepository.Create<IEdi835Repository>();
            this.mockEdi835Repository
                .Setup(_ => _.Get835(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((long e, CancellationToken ct) => FakeEdi(e));
            this.mockEdi835Repository
                 .Setup(_ => _.Add835Status(
                    It.IsAny<long>(),
                    It.IsAny<Edi835StatusCode>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                  ;


            this.mockFileTransferService = this.mockRepository.Create<IFileTransferService>();
            this.mockLogger = this.mockRepository.Create<ILogger<Edi835GenerationMessageProcessor>>();
        }

        #region setup helpers



        private TarPackage FakeTarPackage()
        {
            return new TarPackage { Filename = "somePackage.tar", Stream = GenerateStreamFromString("tar") };
        }
        public static Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }




        private const long Edi835Id_NotFound = 1000;
        private const long Edi835Id_IsTerminal = 1001;
        private const long Edi835Id_Generating = 1200;
        private const long Edi835Id_IsRC = 4000;

        private DS.Edi835 FakeEdi(long edi835Id)
        {

            var boilerPlateEdi835 = new DS.Edi835
            {
                Edi835Id = edi835Id,
                TransactionId = (9990000 + edi835Id),
                UserKey = $"ABC{ edi835Id }",
                OriginCode = "IC",
                ClientCode = "SWK",

                IsTerminalStatus = false,
                StatusCode = Edi835StatusCode.Initiated
            };

            switch (edi835Id)
            {
                case Edi835Id_NotFound:
                    return null;
                case Edi835Id_IsTerminal:
                    boilerPlateEdi835.IsTerminalStatus = true;
                    boilerPlateEdi835.StatusCode = Edi835StatusCode.WillNotReceive835;
                    break;
                case Edi835Id_Generating:
                    boilerPlateEdi835.StatusCode = Edi835StatusCode.Generating;
                    break;
                case Edi835Id_IsRC:
                    boilerPlateEdi835.OriginCode = "RC";
                    boilerPlateEdi835.ClientCode = "TLC";
                    break;
                default:
                    break;
            }
            return boilerPlateEdi835;
        }

        private Edi835GenerationMessageProcessor CreateEdi835GenerationMessageProcessor()
        {
            return new Edi835GenerationMessageProcessor(
                this.mockEdi835ServiceSettings,
                this.mockFileSystem,
                this.mockEdi835CreationService.Object,
                this.mockEdi835Repository.Object,
                this.mockFileTransferService.Object,
                this.mockLogger.Object);
        }

        private string WrapMessage(DS.Edi835 message)
        {
            var wrappedMessage = new MessageWrapper<DS.Edi835>(message);
            return JsonConvert.SerializeObject(wrappedMessage);
        }
        #endregion

        [Fact]
        public async Task ProcessMessage_StateUnderTest_ExpectedBehavior()
        {
            // Arrange
            var edi835GenerationMessageProcessor = this.CreateEdi835GenerationMessageProcessor();

            var edi835 = FakeEdi(1);

            string message = WrapMessage(edi835);
            CancellationToken cancellationToken = default;

            // Act
            await edi835GenerationMessageProcessor.ProcessMessage(
                message,
                cancellationToken);

            // Assert
            this.mockRepository.VerifyAll();
        }

        [Fact]
        public async Task ProcessMessage_StateUnderTest_NotFound_ExpectedBehavior()
        {
            // Arrange
            var edi835GenerationMessageProcessor = this.CreateEdi835GenerationMessageProcessor();

            var edi835 = FakeEdi(1);
            edi835.Edi835Id = Edi835Id_NotFound; // Force _repo.Get835 to return null
            string message = WrapMessage(edi835);
            CancellationToken cancellationToken = default;

            await Assert.ThrowsAsync<ApplicationException>(() => edi835GenerationMessageProcessor.ProcessMessage(
                message,
                cancellationToken));
            // Act

        }

        [Fact]
        public async Task ProcessMessage_StateUnderTest_IsTerminal_ExpectedBehavior()
        {
            // Arrange
            var edi835GenerationMessageProcessor = this.CreateEdi835GenerationMessageProcessor();

            var edi835 = FakeEdi(Edi835Id_IsTerminal);

            string message = WrapMessage(edi835);
            CancellationToken cancellationToken = default;

            // Act
            await edi835GenerationMessageProcessor.ProcessMessage(
                message,
                cancellationToken);
            // Should have LogWarning with message starting with
            // "Edi835Id {Edi835Id}  is terminal  "

            // Assert
            mockLogger.Verify(x => x.Log(
                It.Is<LogLevel>(ll => ll == LogLevel.Warning),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>())
            ,Times.Once
            );
        }

        [Fact]
        public async Task ProcessMessage_StateUnderTest_CannotBeGenerated_ExpectedBehavior()
        {
            // Arrange
            var edi835GenerationMessageProcessor = this.CreateEdi835GenerationMessageProcessor();

            var edi835 = FakeEdi(Edi835Id_Generating);
            string message = WrapMessage(edi835);
            CancellationToken cancellationToken = default;

            // Act
            await edi835GenerationMessageProcessor.ProcessMessage(
                message,
                cancellationToken);
            // Should have LogWarning with message starting with
            // "Edi835Id {Edi835Id}  has already been processed "
            // Assert
            mockLogger.Verify(x => x.Log(
                It.Is<LogLevel>(ll => ll == LogLevel.Warning),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>())
            , Times.Once
            );
        }


        [Fact]
        public async Task ProcessMessage_StateUnderTest_ArchiveRedCardDocs_ExpectedBehavior()
        {
            // Arrange
            this.mockEdi835ServiceSettings = new Edi835ServiceSettings
            {
                TarFileTransfer = new TarFileTransferSettings { Enabled = true },
                ArchiveRedCardDocsAfterProcessing = true,
            };
            var edi835GenerationMessageProcessor = this.CreateEdi835GenerationMessageProcessor();

            var edi835 = FakeEdi(Edi835Id_IsRC);
            edi835.StatusCode = Edi835StatusCode.Initiated;

            string message = WrapMessage(edi835);

            this.mockFileSystem.DirectoryMoq
                .Setup(_ => _.Exists(mockEdi835ServiceSettings.DocsArchiveFolder))
                .Returns(true);
            this.mockFileSystem.FileMoq
                .SetupSequence(_ => _.Exists(It.IsRegex("docs$")))
                .Returns(true)      // docsFilename
                .Returns(false);    // archivedDocsFilename
            this.mockFileSystem.FileMoq
                .Setup(_ => _.Exists(It.IsRegex("lock$")))
                .Returns(false);    // docsFilenameLocked
            this.mockFileSystem.FileMoq
                .Setup(_ => _.Move(It.IsAny<string>(), It.IsAny<string>()));

            // Act
            await edi835GenerationMessageProcessor.ProcessMessage(
                message,
                new CancellationToken());

            // Assert
            this.mockFileSystem.FileMoq.Verify(_ => _.Move(It.IsRegex("lock$"), It.IsRegex("docs$")), Times.Once);
            this.mockFileSystem.FileMoq.Verify(_ => _.Move(It.IsRegex("docs$"), It.IsRegex("lock$")), Times.Once);
        }

        [Fact]
        public async Task ProcessMessage_StateUnderTest_ArchiveRedCardDocs_ShouldCreatedArchiveFolder_ExpectedBehavior()
        {
            // Arrange
            this.mockEdi835ServiceSettings = new Edi835ServiceSettings
            {
                TarFileTransfer = new TarFileTransferSettings { Enabled = true },
                ArchiveRedCardDocsAfterProcessing = true,
            };
            var edi835GenerationMessageProcessor = this.CreateEdi835GenerationMessageProcessor();

            var edi835 = FakeEdi(Edi835Id_IsRC);
            edi835.StatusCode = Edi835StatusCode.Initiated;

            string message = WrapMessage(edi835);
            this.mockFileSystem.DirectoryMoq
                .Setup(_ => _.Exists(mockEdi835ServiceSettings.DocsArchiveFolder))
                .Returns(true);
            this.mockFileSystem.FileMoq
                .SetupSequence(_ => _.Exists(It.IsRegex("docs$")))
                .Returns(true)      // docsFilename
                .Returns(false);    // archivedDocsFilename
            this.mockFileSystem.FileMoq
                .Setup(_ => _.Exists(It.IsRegex("lock$")))
                .Returns(false);    // docsFilenameLocked
            this.mockFileSystem.FileMoq
                .Setup(_ => _.Move(It.IsAny<string>(), It.IsAny<string>()));

            // Act
            await edi835GenerationMessageProcessor.ProcessMessage(
                message,
                new CancellationToken());

            // Assert
            this.mockFileSystem.DirectoryMoq.Verify(_ => _.CreateDirectory(It.IsRegex(@"archive[\/\\]docs[\/\\]0001[\/\\]01[\/\\]01")), Times.Once);
            this.mockFileSystem.FileMoq.Verify(_ => _.Move(It.IsRegex("lock$"), It.IsRegex("docs$")), Times.Once);
            this.mockFileSystem.FileMoq.Verify(_ => _.Move(It.IsRegex("docs$"), It.IsRegex("lock$")), Times.Once);
        }

        [Fact]
        public async Task ProcessMessage_StateUnderTest_ArchiveRedCardDocs_ShouldUseExistingArchiveFolder_ExpectedBehavior()
        {
            // Arrange
            this.mockEdi835ServiceSettings = new Edi835ServiceSettings
            {
                TarFileTransfer = new TarFileTransferSettings { Enabled = true },
                DocsArchiveFolder = ".",
                ArchiveRedCardDocsAfterProcessing = true,
            };
            var edi835GenerationMessageProcessor = this.CreateEdi835GenerationMessageProcessor();

            var edi835 = FakeEdi(Edi835Id_IsRC);
            edi835.StatusCode = Edi835StatusCode.Initiated;

            string message = WrapMessage(edi835);

            var preexistingArchiveFolder = this.mockFileSystem.Path.Combine(mockEdi835ServiceSettings.DocsArchiveFolder, "0001", "01", "01");
 

            this.mockFileSystem.DirectoryMoq
                .Setup(_ => _.Exists(mockEdi835ServiceSettings.DocsArchiveFolder))
                .Returns(true);
            this.mockFileSystem.DirectoryMoq
                .Setup(_ => _.Exists(preexistingArchiveFolder))
                .Returns(true);
            this.mockFileSystem.FileMoq
                .SetupSequence(_ => _.Exists(It.IsRegex("docs$")))
                .Returns(true)      // docsFilename
                .Returns(false);    // archivedDocsFilename
            this.mockFileSystem.FileMoq
                .Setup(_ => _.Exists(It.IsRegex("lock$")))
                .Returns(false);    // docsFilenameLocked
            this.mockFileSystem.FileMoq
                .Setup(_ => _.Move(It.IsAny<string>(), It.IsAny<string>()));

            // Act
            await edi835GenerationMessageProcessor.ProcessMessage(
                message,
                new CancellationToken());

            // Assert
            this.mockFileSystem.DirectoryMoq.Verify(_ => _.CreateDirectory(It.IsRegex(@"01$")), Times.Never);
            this.mockFileSystem.FileMoq.Verify(_ => _.Move(It.IsRegex("lock$"), It.IsRegex("docs$")), Times.Once);
            this.mockFileSystem.FileMoq.Verify(_ => _.Move(It.IsRegex("docs$"), It.IsRegex("lock$")), Times.Once);
        }




    }
}
