using System;
using System.Data.Common;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Dapper;
using VPay.DocSys.Data;
using VPay.DocSys.Generation.Common.Models.Sql;
using VPay.DocSys.Generation.Data;
using Xunit;

namespace VPay.DocSys.Generation.Tests
{
    public class DocumentPaymentTransactionRepositoryTests
    {
        [Fact]
        public async Task GetPaymentTransactionsAsync_ShouldReturnPaymentTransaction_WhenDataExists()
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
            var mockDbConnection = new Mock<DbConnection>();
            mockDbConnection
                .SetupDapperAsync(conn => conn.QueryFirstOrDefaultAsync<PaymentTransaction>(
                    It.IsAny<CommandDefinition>()))
                .ReturnsAsync(expectedPaymentTransaction);

            var mockConnection = new Mock<IDocSysConnection>();
            mockConnection
                .Setup(c => c.Policies)
                .Returns(new DocSysPolicyHolder(NullLogger<DocSysPolicyHolder>.Instance));
            mockConnection
                .Setup(c => c.GetDbConnection())
                .Returns(mockDbConnection.Object);

            var repository = new DocumentPaymentTransactionRepository(mockConnection.Object, NullLogger<DocumentPaymentTransactionRepository>.Instance);


            // Act
            var result = await repository.GetPaymentTransactionsAsync(transactionId, TestContext.Current.CancellationToken);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedPaymentTransaction.TransactionId, result.TransactionId);
            Assert.Equal(expectedPaymentTransaction.ClientCode, result.ClientCode);
            Assert.Equal(expectedPaymentTransaction.BillingEntityCode, result.BillingEntityCode);
            Assert.Equal(expectedPaymentTransaction.PaymentId, result.PaymentId);
        }

        [Fact]
        public async Task GetPaymentTransactionsAsync_ShouldThrowException_WhenConnectionFails()
        {
            // Arrange
            var transactionId = 12345L;
            var mockConnection = new Mock<IDocSysConnection>();
            mockConnection
                .Setup(c => c.Policies)
                .Returns(new DocSysPolicyHolder(NullLogger<DocSysPolicyHolder>.Instance));
            mockConnection
                .Setup(c => c.GetDbConnection())
                .Throws(new Exception("Database connection failed"));

            var repository = new DocumentPaymentTransactionRepository(mockConnection.Object, NullLogger<DocumentPaymentTransactionRepository>.Instance);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(
                () => repository.GetPaymentTransactionsAsync(transactionId, TestContext.Current.CancellationToken));
        }
    }
}
