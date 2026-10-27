using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using VPay.Gateway.Common.Documents;
using VPay.Gateway.Common.Documents.Models;
using Xunit;

namespace VPay.Gateway.Common.Documents.Tests;
public class GetPaymentByTransactionIdQueryHandlerTests
{
    private readonly Mock<ILogger<GetPaymentByTransactionIdQueryHandler>> _mockLogger;
    private readonly Mock<IDocumentsApi> _mockDocumentsApi;
    private readonly GetPaymentByTransactionIdQueryHandler _handler;

    public GetPaymentByTransactionIdQueryHandlerTests()
    {
        _mockLogger = new Mock<ILogger<GetPaymentByTransactionIdQueryHandler>>();
        _mockDocumentsApi = new Mock<IDocumentsApi>();
        _handler = new GetPaymentByTransactionIdQueryHandler(_mockLogger.Object, _mockDocumentsApi.Object);
    }

    [Fact]
    public async Task Handle_WhenTransactionExists_ReturnsTransactionPayment()
    {
        // Arrange
        var transactionId = 12345L;
        var expectedPayment = new TransactionPayment
        {
            Id = 1,
            TransactionId = transactionId,
            ClientCode = "Client1",
            BillingEntityCode = "Entity1",
            PaymentId = "Payment123"
        };

        _mockDocumentsApi
            .Setup(api => api.DocumentPaymentByTransactionID(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPayment);

        var query = new GetDocumentPaymentDetailByTransactionIDQuery(transactionId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedPayment);
    }

    [Fact]
    public async Task Handle_WhenTransactionDoesNotExist_ReturnsNull()
    {
        // Arrange
        var transactionId = 12345L;

        _mockDocumentsApi
            .Setup(api => api.DocumentPaymentByTransactionID(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TransactionPayment?)null);

        var query = new GetDocumentPaymentDetailByTransactionIDQuery(transactionId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Constructor_WhenLoggerIsNull_ThrowsArgumentNullException()
    {
        // Act & Assert  
        Assert.Throws<ArgumentNullException>("logger", () =>
        {
            var handler = new GetPaymentByTransactionIdQueryHandler(null!, _mockDocumentsApi.Object);
        });
    }

    [Fact]
    public void Constructor_WhenDocumentsApiIsNull_ThrowsArgumentNullException()
    {
        // Act
        var exception = Assert.Throws<ArgumentNullException>(() =>
        {
            _ = new GetPaymentByTransactionIdQueryHandler(_mockLogger.Object, null!);
        });

        // Assert
        exception.ParamName.Should().Be("documentApi");
    }
}
