
using Moq;
using Xunit;
using VPay.Gateway.Data.Sql.Transactions;
using VPay.Gateway.Data.Connection;
using Microsoft.Data.SqlClient;
using VPay.Gateway.Data.Paging;

namespace VPay.Gateway.Data.Sql.Tests.Transactions;

public class GetTransactionsByTrn02QueryTests
    {
        [Fact]
        public async Task Handle_ValidRequest_ReturnsPagedResult()
        {
            // Arrange
            var mockDataConnection = new Mock<IDataConnection<SqlConnection>>();
            var handler = new GetTransactionsByTrn02.Handler(mockDataConnection.Object);

            var query = new GetTransactionsByTrn02.Query(
                StartDate: DateTime.UtcNow.AddDays(-1),
                EndDate: DateTime.UtcNow,
                Trn02: "TRN02",
                ProviderTin: "123456789",
                Page: 1,
                PageSize: 10
            );

            var expectedItems = new List<TransactionsTrn02Details>
            {
                new TransactionsTrn02Details
                {
                    VpayTransactionId = 1,
                    PaymentId = "Payment1",
                    PaymentType = "Type1",
                    PaymentDate = DateTime.UtcNow,
                    ProviderTin = "123456789",
                    Trn02 = "TRN02",
                    ClientID = "Client1",
                    BillingEntity = "Entity1"
                }
            };

            var expectedResult = new PagedResult<TransactionsTrn02Details>(
                items: expectedItems,
                currentPage: 1,
                pageSize: 10,
                totalRows: 1
            );

            mockDataConnection
                .Setup(dc => dc.GetPagedResultAsync<TransactionsTrn02Details>(
                    It.IsAny<string>(),
                    It.IsAny<object>(),
                    It.IsAny<CancellationToken>()
                ))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedResult.Items, result.Items);
            Assert.Equal(expectedResult.CurrentPage, result.CurrentPage);
            Assert.Equal(expectedResult.PageSize, result.PageSize);
            Assert.Equal(expectedResult.TotalRows, result.TotalRows);
        }

        [Fact]
        public async Task Handle_NullRequest_ThrowsArgumentNullException()
        {
            // Arrange
            var mockDataConnection = new Mock<IDataConnection<SqlConnection>>();
            var handler = new GetTransactionsByTrn02.Handler(mockDataConnection.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => handler.Handle(null, CancellationToken.None));
        }
    }


