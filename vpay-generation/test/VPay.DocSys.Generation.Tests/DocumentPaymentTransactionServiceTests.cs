using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using VPay.DocSys.Generation;
using VPay.DocSys.Generation.Common.Models.Sql;
using VPay.DocSys.Generation.Data;
using Xunit;

namespace VPay.DocSys.Generation.Tests
{
    public class DocumentPaymentTransactionServiceTests
    {
        private readonly Mock<IDb2TransactionRepository> _mockRepository;
        private readonly Mock<ILogger<DocumentPaymentTransactionService>> _mockLogger;
        private readonly DocumentPaymentTransactionService _service;
        private Mock<ILogger<DocumentPaymentTransactionService>> _logger = new Mock<ILogger<DocumentPaymentTransactionService>>(MockBehavior.Loose);

        public DocumentPaymentTransactionServiceTests()
        {
            _mockRepository = new Mock<IDb2TransactionRepository>();
            _mockLogger = new Mock<ILogger<DocumentPaymentTransactionService>>();

            // Pass the mocked logger directly
            _service = new DocumentPaymentTransactionService(_mockRepository.Object, _logger.Object);
        }

        [Fact]
        public async Task GetDocumentPaymentDetails_ReturnsPaymentTransaction_WhenTransactionExists()
        {
            // Arrange
            var transactionId = 12345L;
            var expectedPaymentTransaction = new PaymentTransaction
            {
                Id = 1,
                TransactionId = transactionId,
                ClientCode = "Client123",
                BillingEntityCode = "Entity456",
                PaymentId = "Payment789"
            };

            _mockRepository
                .Setup(repo => repo.GetPayerInfoByTransactionIdAsync(transactionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedPaymentTransaction);

            // Act
            var result = await _service.GetDocumentPaymentDetails(transactionId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedPaymentTransaction.Id, result.Id);
            Assert.Equal(expectedPaymentTransaction.TransactionId, result.TransactionId);
            Assert.Equal(expectedPaymentTransaction.ClientCode, result.ClientCode);
            Assert.Equal(expectedPaymentTransaction.BillingEntityCode, result.BillingEntityCode);
            Assert.Equal(expectedPaymentTransaction.PaymentId, result.PaymentId);

            _mockRepository.Verify(repo => repo.GetPayerInfoByTransactionIdAsync(transactionId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetDocumentPaymentDetails_ReturnsNull_WhenTransactionDoesNotExist()
        {
            // Arrange
            var transactionId = 12345L;

            _mockRepository
                .Setup(repo => repo.GetPayerInfoByTransactionIdAsync(transactionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((PaymentTransaction)null);

            // Act
            var result = await _service.GetDocumentPaymentDetails(transactionId);

            // Assert
            Assert.Null(result);
            _mockRepository.Verify(repo => repo.GetPayerInfoByTransactionIdAsync(transactionId, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
