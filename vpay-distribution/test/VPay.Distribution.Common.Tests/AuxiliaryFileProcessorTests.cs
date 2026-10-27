using System.IO.Abstractions;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using VPay.DocSys.Common;
using VPay.DocSys.Data;

namespace VPay.Distribution.Common.Tests
{
    public class AuxiliaryFileProcessorTests
    {
        private readonly Mock<ISshOperations> _sshOperationsMock = new();
        private readonly Mock<AuxiliaryScpSettings> _scpSettingsMock = new();
        private readonly Mock<IRedCardRepository> _redCardRepositoryMock = new();
        private readonly Mock<IFileSystem> _fileSystemMock = new();
        private readonly Mock<ILogger<AuxiliaryFileProcessor>> _loggerMock = new();

        private AuxiliaryFileProcessor CreateProcessor(bool transferFiles = true)
        {
            return new AuxiliaryFileProcessor(
                _sshOperationsMock.Object,
                _scpSettingsMock.Object,
                _redCardRepositoryMock.Object,
                _fileSystemMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task Process_FileAlreadyTransferred_ThrowsError()
        {
            var message = new AuxiliaryMessage { StatusFileName = "file.sts", PaymentStatusFileId = 1 };
            _redCardRepositoryMock.Setup(x => x.GetFileStatus(message.PaymentStatusFileId, default))
                .ReturnsAsync(RedCardFileStatusCode.AuxiliaryFileTransferred);

            var processor = CreateProcessor();

            Func<Task> act = async () => await processor.Process(message);
            await act.Should().ThrowAsync<Exception>();
        }

        [Fact]
        public async Task Process_TransfersFilesAndUpdatesStatus()
        {
            var message = new AuxiliaryMessage { StatusFileName = "file.sts", PaymentStatusFileId = 2 };
            _redCardRepositoryMock.Setup(x => x.GetFileStatus(message.PaymentStatusFileId, default))
                .ReturnsAsync(RedCardFileStatusCode.NotFound);

            _fileSystemMock.Setup(x => x.Path.GetFileName(It.IsAny<string>())).Returns("file.aux");
            _sshOperationsMock.Setup(x => x.TransferFileAsync(It.IsAny<string>(), It.IsAny<AuxiliaryScpSettings>()))
                .Returns(Task.CompletedTask);
            _sshOperationsMock.Setup(x => x.TouchControlFileOnServerAsync(It.IsAny<string>(), It.IsAny<AuxiliaryScpSettings>()))
                .Returns(Task.CompletedTask);

            var processor = CreateProcessor();

            var result = await processor.Process(message);

            _sshOperationsMock.Verify(x => x.TransferFileAsync(It.IsAny<string>(), It.IsAny<AuxiliaryScpSettings>()), Times.Once);
            _sshOperationsMock.Verify(x => x.TouchControlFileOnServerAsync(It.IsAny<string>(), It.IsAny<AuxiliaryScpSettings>()), Times.Once);
            _redCardRepositoryMock.Verify(x => x.UpdateFileStatus(message.PaymentStatusFileId, RedCardFileStatusCode.AuxiliaryFileTransferred, default), Times.Once);
            Assert.Contains("file.aux", result.FileNames);
            Assert.Contains("file.aux.ctrl", result.ControlFileNames);
        }

        [Fact]
        public async Task Process_TransferFilesFails_ThrowsError()
        {
            var message = new AuxiliaryMessage { StatusFileName = "file.sts", PaymentStatusFileId = 3 };
            _redCardRepositoryMock.Setup(x => x.GetFileStatus(message.PaymentStatusFileId, default))
                .ReturnsAsync(RedCardFileStatusCode.NotFound);

            _fileSystemMock.Setup(x => x.Path.GetFileName(It.IsAny<string>())).Returns("file.aux");
            _sshOperationsMock.Setup(x => x.TransferFileAsync(It.IsAny<string>(), It.IsAny<AuxiliaryScpSettings>()))
                .ThrowsAsync(new InvalidOperationException("Transfer failed"));

            var processor = CreateProcessor();

            Func<Task> act = async () => await processor.Process(message);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }
    }
}
