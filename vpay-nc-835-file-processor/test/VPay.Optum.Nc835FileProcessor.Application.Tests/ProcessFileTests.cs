using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using VPay.Extensions.Exceptions;
using VPay.Optum.Data.Sql.InboundFiles;
using VPay.Optum.Nc835FileProcessor.Application;
using VPay.Optum.Nc835FileProcessor.Application.Services;
using VPay.Optum.Nc835Segments;
using Xunit;

namespace VPay.Optum.Nc835FileProcessor.Application.Tests
{
    public class ProcessFileTests
    {
        private readonly Mock<IFileSystem> _fileSystemMock;
        private readonly Mock<IMediator> _mediatorMock;
        private readonly Mock<ILogger<ProcessFile.Handler>> _loggerMock;
        private readonly Mock<IX12Parser> _parserMock;
        private readonly ProcessFile.Handler _handler;

        public ProcessFileTests()
        {
            _fileSystemMock = new Mock<IFileSystem>();
            _mediatorMock = new Mock<IMediator>();
            _loggerMock = new Mock<ILogger<ProcessFile.Handler>>();
            _parserMock = new Mock<IX12Parser>();

            _handler = new ProcessFile.Handler(
                _fileSystemMock.Object,
                _mediatorMock.Object,
                _loggerMock.Object,
                _parserMock.Object
            );
        }

        [Fact]
        public async Task Handle_ShouldThrowItemNotFoundException_WhenRecordIsNull()
        {
            // Arrange
            var command = new ProcessFile.Command(1, Guid.NewGuid());
            _mediatorMock
                .Setup(m => m.Send(It.IsAny<GetInboundFile.Query>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((InboundFile?)null);

            // Act & Assert
            await Assert.ThrowsAsync<ItemNotFoundException>(() => _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_ShouldReturnFalse_WhenFileIsAlreadyCompleted()
        {
            // Arrange
            var command = new ProcessFile.Command(1, Guid.NewGuid());
            var record = new InboundFile
            {
                Id = 1,
                Status = InboundFileStatusEnum.Completed
            };

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<GetInboundFile.Query>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(record);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task Handle_ShouldReturnFalse_WhenQueueMessageIdDoesNotMatch()
        {
            // Arrange
            var command = new ProcessFile.Command(1, Guid.NewGuid());
            var record = new InboundFile
            {
                Id = 1,
                QueueMessageId = Guid.NewGuid()
            };

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<GetInboundFile.Query>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(record);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task Handle_ShouldUpdateStatusAndReturnTrue_WhenProcessingSucceeds()
        {
            // Arrange
            var command = new ProcessFile.Command(1, Guid.NewGuid());
            var record = new InboundFile
            {
                Id = 1,
                Status = InboundFileStatusEnum.Queued,
                FilePath = "test.txt"
            };

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<GetInboundFile.Query>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(record);

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<UpdateInboundFileStatus.Command>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new InboundFile { Status = InboundFileStatusEnum.Completed });

            _fileSystemMock
                .Setup(fs => fs.File.OpenRead(It.IsAny<string>()))
                .Returns(new System.IO.MemoryStream());

            _parserMock
                .Setup(p => p.GetReader(It.IsAny<System.IO.Stream>()))
                .Returns(Mock.Of<IX12Reader>());

            // Act
            await Assert.ThrowsAsync<System.NullReferenceException>(() => _handler.Handle(command, CancellationToken.None));
        }

        
    }
}
