using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using VPay.Extensions.Exceptions;
using VPay.Optum.Data.Sql.InboundFiles;
using VPay.Optum.Data.Sql.PrfFiles;
using VPay.Optum.Http.Loaf.Models;
using VPay.Optum.Nc835Record.Application;
using Xunit;

namespace VPay.Optum.Nc835Record.Application.Tests
{
    public class ProcessIndividualRecordTest
    {
        private readonly Mock<IMediator> _mediatorMock;
        private readonly Mock<ILogger<ProcessIndividualRecord.Handler>> _loggerMock;
        private readonly ProcessIndividualRecord.Handler _handler;

        public ProcessIndividualRecordTest()
        {
            _mediatorMock = new Mock<IMediator>();
            _loggerMock = new Mock<ILogger<ProcessIndividualRecord.Handler>>();
            _handler = new ProcessIndividualRecord.Handler(_mediatorMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldThrowItemNotFoundException_WhenRecordIsNull()
        {
            // Arrange
            var command = new ProcessIndividualRecord.Command(
                new Nc835RecordNotification(
                    1, // InboundFileRecordId
                    string.Empty, // TransactionId
                    string.Empty, // PaymentType
                    string.Empty, // FromRouting
                    string.Empty, // FromAccount
                    string.Empty, // ToRouting
                    string.Empty, // ToAccount
                    string.Empty  // PayerTaxId
                ),
                null
            );
            _mediatorMock
                .Setup(m => m.Send(It.IsAny<GetInboundFileRecord.Query>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((InboundFileRecord?)null);

            // Act & Assert
            await Assert.ThrowsAsync<ItemNotFoundException>(() => _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_ShouldReturnFalse_WhenRecordIsAlreadyCompleted()
        {
            // Arrange
            var record = new InboundFileRecord { Status = InboundFileRecordStatusEnum.Completed }; // Fixed type to match InboundFileRecordStatusEnum
            var command = new ProcessIndividualRecord.Command(
                new Nc835RecordNotification(
                    1, // InboundFileRecordId
                    string.Empty, // TransactionId
                    string.Empty, // PaymentType
                    string.Empty, // FromRouting
                    string.Empty, // FromAccount
                    string.Empty, // ToRouting
                    string.Empty, // ToAccount
                    string.Empty  // PayerTaxId
                ),
                null
            );

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<GetInboundFileRecord.Query>(), It.IsAny<CancellationToken>()))
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
            var record = new InboundFileRecord { QueueMessageId = Guid.NewGuid() };
            var command = new ProcessIndividualRecord.Command(
                new Nc835RecordNotification(
                    1, // InboundFileRecordId
                    string.Empty, // TransactionId
                    string.Empty, // PaymentType
                    string.Empty, // FromRouting
                    string.Empty, // FromAccount
                    string.Empty, // ToRouting
                    string.Empty, // ToAccount
                    string.Empty  // PayerTaxId
                ),
                Guid.NewGuid()
            );

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<GetInboundFileRecord.Query>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(record);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task Handle_ShouldProcessRecordSuccessfully()
        {
            // Arrange
            var record = new InboundFileRecord { Status = InboundFileRecordStatusEnum.Pending }; // Fixed type to match InboundFileRecordStatusEnum
            var command = new ProcessIndividualRecord.Command(
                new Nc835RecordNotification(
                    1, // InboundFileRecordId
                    "12345", // TransactionId
                    string.Empty, // PaymentType
                    "111111", // FromRouting
                    "222222", // FromAccount
                    "333333", // ToRouting
                    "444444", // ToAccount
                    "555555"  // PayerTaxId
                ),
                Guid.NewGuid()
            );

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<GetInboundFileRecord.Query>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(record);

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<UpdateInboundFileRecordStatus.Command>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new InboundFileRecord { Status = InboundFileRecordStatusEnum.Started }); // Fixed type to match InboundFileRecordStatusEnum

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<CreateLoafTransaction.Command>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true); // Fixed to return a Task<bool> as expected

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<TieTransactionTo835Record.Command>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PrfTransaction()); // Fixed to return a PrfTransaction as expected

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<ValidateTransactionDetails.Command>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result);
        }
    }

    public static class LoggerExtensions
    {
        public static void VerifyLog(this Mock<ILogger> logger, LogLevel level, Times times)
        {
            logger.Verify(x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.IsAny<object>(),
                It.IsAny<Exception>(),
                (Func<object, Exception?, string>)It.IsAny<object>()),
                times);
        }
    }
}
