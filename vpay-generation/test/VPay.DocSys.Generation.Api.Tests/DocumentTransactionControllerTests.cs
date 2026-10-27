using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using VPay.DocSys.Generation.Api.Controllers;
using VPay.DocSys.Generation.Common.Models.Sql;
using Xunit;

namespace VPay.DocSys.Generation.Api.Tests.Controllers
{
    public class DocumentTransactionControllerTests
    {
        private readonly Mock<ILogger<DocumentTransactionController>> _mockLogger;
        private readonly Mock<IDocumentPaymentTransactionService> _mockService;
        private readonly DocumentTransactionController _controller;

        public DocumentTransactionControllerTests()
        {
            _mockLogger = new Mock<ILogger<DocumentTransactionController>>();
            _mockService = new Mock<IDocumentPaymentTransactionService>();
            _controller = new DocumentTransactionController(_mockLogger.Object, _mockService.Object);
        }

        [Fact]
        public async Task GetPaymentByTransaction_ReturnsOkResult_WithPaymentTransaction()
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

            _mockService
                .Setup(service => service.GetDocumentPaymentDetails(transactionId))
                .ReturnsAsync(expectedPaymentTransaction);

            // Act
            var result = await _controller.GetPaymentByTransaction(transactionId, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var actualPaymentTransaction = Assert.IsType<PaymentTransaction>(okResult.Value);
            Assert.Equal(expectedPaymentTransaction.Id, actualPaymentTransaction.Id);
            Assert.Equal(expectedPaymentTransaction.TransactionId, actualPaymentTransaction.TransactionId);
            Assert.Equal(expectedPaymentTransaction.ClientCode, actualPaymentTransaction.ClientCode);
            Assert.Equal(expectedPaymentTransaction.BillingEntityCode, actualPaymentTransaction.BillingEntityCode);
            Assert.Equal(expectedPaymentTransaction.PaymentId, actualPaymentTransaction.PaymentId);

            _mockService.Verify(service => service.GetDocumentPaymentDetails(transactionId), Times.Once);
        }

        [Fact]
        public async Task GetPaymentByTransaction_ReturnsNotFound_WhenServiceReturnsNull()
        {
            // Arrange
            var transactionId = 12345L;

            _mockService
                .Setup(service => service.GetDocumentPaymentDetails(transactionId))
                .ReturnsAsync((PaymentTransaction)null);

            // Act
            var result = await _controller.GetPaymentByTransaction(transactionId, CancellationToken.None);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result); // Updated to NotFoundObjectResult
            Assert.NotNull(notFoundResult.Value); // Ensure the returned object is not null
            _mockService.Verify(service => service.GetDocumentPaymentDetails(transactionId), Times.Once);
        }
    }
}
