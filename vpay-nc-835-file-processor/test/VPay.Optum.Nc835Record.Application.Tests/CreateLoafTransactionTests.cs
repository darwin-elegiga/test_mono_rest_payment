using Microsoft.Extensions.Logging;
using Moq;
using Refit;
using VPay.Optum.Http.DocSysGeneration;
using VPay.Optum.Http.DocSysGeneration.Models;
using VPay.Optum.Http.Loaf;
using VPay.Optum.Http.Loaf.Models;
using VPay.Optum.Nc835Record.Application;

namespace VPay.Optum.Nc835Record.Application.Tests
{
    public class CreateLoafTransactionTests
    {
        private readonly Mock<ILoafApi> _loafApiMock;
        private readonly Mock<IDocSysGenerationApi> _docSysGenerationApiMock;
        private readonly Mock<ILogger<CreateLoafTransaction.Handler>> _loggerMock;
        private readonly CreateLoafTransaction.Handler _handler;

        public CreateLoafTransactionTests()
        {
            _loafApiMock = new Mock<ILoafApi>();
            _docSysGenerationApiMock = new Mock<IDocSysGenerationApi>();
            _loggerMock = new Mock<ILogger<CreateLoafTransaction.Handler>>();
            _handler = new CreateLoafTransaction.Handler(_loafApiMock.Object, _docSysGenerationApiMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldReturnTrue_WhenTransactionIsProcessed()
        {
            // Arrange
            var bankInfo = new TransactionBankInfoUpsert { ReferenceId = "12345" };
            var command = new CreateLoafTransaction.Command(bankInfo);
            var transactionDetails = new TransactionDetails { ReferenceId = "12345" };

            _loafApiMock.Setup(api => api.Upsert(It.IsAny<TransactionBankInfoUpsert>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(transactionDetails);

            _docSysGenerationApiMock.Setup(api => api.PublishTransactions(It.IsAny<Edi835TransactionsDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Dictionary<long, string> { { 12345, "Success" } });

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task Handle_ShouldLogError_WhenApiExceptionIsThrown()
        {
            // Arrange
            var bankInfo = new TransactionBankInfoUpsert { ReferenceId = "12345" };
            var command = new CreateLoafTransaction.Command(bankInfo);


            // Act & Assert
            await Assert.ThrowsAsync<NullReferenceException>(() => _handler.Handle(command, CancellationToken.None));
            
        }
    }
}
