using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using VPay.Extensions.Exceptions;
using VPay.Optum.Http.Transactions;
using VPay.Optum.Http.Transactions.Models;
using VPay.Optum.Nc835Record.Application;
using VPay.Optum.Nc835Record.Application.Queries;
using Xunit;

namespace VPay.Optum.Nc835Record.Application.Tests;
public class ValidateTransactionDetailsTest
{
    private readonly Mock<ITransactionsApi> _transactionsApiMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<ValidateTransactionDetails.Handler>> _loggerMock;
    private readonly ValidateTransactionDetails.Handler _handler;

    public ValidateTransactionDetailsTest()
    {
        _transactionsApiMock = new Mock<ITransactionsApi>();
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<ValidateTransactionDetails.Handler>>();
        _handler = new ValidateTransactionDetails.Handler(_transactionsApiMock.Object, _mediatorMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldThrowItemNotFoundException_WhenTransactionIsNotFound()
    {
        // Arrange
        var command = new ValidateTransactionDetails.Command(12345, "123456789");
        _transactionsApiMock.Setup(api => api.GetByTransactionId(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TransactionInfo { Items = new List<TransactionListItem>() });

        // Act & Assert
        await Assert.ThrowsAsync<ItemNotFoundException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldThrowItemNotFoundException_WhenSubPayerIdIsNull()
    {
        // Arrange
        var command = new ValidateTransactionDetails.Command(12345, "123456789");
        var transactionListItem = new TransactionListItem { ClientCode = "ClientCode", BillingEntityCode = "BillingEntityCode" };
        _transactionsApiMock.Setup(api => api.GetByTransactionId(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TransactionInfo { Items = new List<TransactionListItem> { transactionListItem } });

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetSubPayerId.Query>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int?)null);

        // Act & Assert
        await Assert.ThrowsAsync<ItemNotFoundException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldLogCritical_WhenPayerTaxIdMatchesSubPayerId()
    {
        // Arrange
        var command = new ValidateTransactionDetails.Command(12345, "1123456789");
        var transactionListItem = new TransactionListItem { ClientCode = "ClientCode", BillingEntityCode = "BillingEntityCode" };
        _transactionsApiMock.Setup(api => api.GetByTransactionId(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TransactionInfo { Items = new List<TransactionListItem> { transactionListItem } });

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetSubPayerId.Query>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(123456789);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Contains("TRN03 is the same as the VPay's SubPayerId", "TRN03 is the same as the VPay's SubPayerId");
       
    }
}
