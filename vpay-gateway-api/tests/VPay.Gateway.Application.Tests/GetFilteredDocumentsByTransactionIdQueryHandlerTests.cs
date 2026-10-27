using MediatR;
using Moq;
using VPay.Extensions.Exceptions;
using VPay.Gateway.Common.Documents;
using VPay.Gateway.Common.Documents.Models;
using VPay.Gateway.Common.Kubera;
using VPay.Gateway.Common.Kubera.Models;
using Xunit;

namespace VPay.Gateway.Application.Tests;

public class GetFilteredDocumentsByTransactionIdQueryHandlerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<IDocumentsApi> _documentsApiMock = new();
    private readonly Mock<IUserInfo> _userInfoMock = new();
    private static readonly string[] _collection = ["Generic", "Pdf", "Limited", "ClearedCheck"];

    private GetFilteredDocumentsByTransactionIdQueryHandler CreateHandler() =>
        new(_mediatorMock.Object, _documentsApiMock.Object, _userInfoMock.Object);

    [Fact]
    public async Task Handle_ReturnsNull_WhenTransactionNotFound()
    {
        // Arrange
        var handler = CreateHandler();
        var query = new GetFilteredDocumentsByTransactionIdQuery(12345L);

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetTransactionByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction)null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_ThrowsNotAuthorizedException_WhenUserCannotAccessTransaction()
    {
        // Arrange
        var handler = CreateHandler();
        var query = new GetFilteredDocumentsByTransactionIdQuery(12345L);
        var transaction = new Transaction
        {
            ClientCode = "client",
            BillingEntityCode = "entity",
            PaymentTypeCode = "type"
        };

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetTransactionByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);
        _userInfoMock.Setup(u => u.CanAccess(transaction.ClientCode, transaction.BillingEntityCode, transaction.PaymentTypeCode))
            .Returns(false);

        // Act & Assert
        await Assert.ThrowsAsync<NotAuthorizedException>(() => handler.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ReturnsFilteredDocuments_WhenUserCanAccessTransaction()
    {
        // Arrange
        var handler = CreateHandler();
        var query = new GetFilteredDocumentsByTransactionIdQuery(12345L);
        var transaction = new Transaction
        {
            ClientCode = "client",
            BillingEntityCode = "entity",
            PaymentTypeCode = "type"
        };
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetTransactionByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);
        _userInfoMock.Setup(u => u.CanAccess(transaction.ClientCode, transaction.BillingEntityCode, transaction.PaymentTypeCode))
            .Returns(true);

        var archiveDetails = new List<ArchiveDetail>
        {
            new() { FileDetailType = "Generic" },
            new() { FileDetailType = "Pdf" },
            new() { FileDetailType = "Limited" },
            new() { FileDetailType = "ClearedCheck" },
            new() { FileDetailType = "Other" }
        };
        var documentList = new List<ArchiveFile>
        {
            new() { ArchiveDetails = archiveDetails }
        };

        _documentsApiMock.Setup(d => d.ListDocuments(query.TransactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([.. documentList]);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.All(result, d => Assert.Contains(d.FileDetailType, _collection));
        Assert.DoesNotContain(result, d => d.FileDetailType == "Other");
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenDependenciesAreNull()
    {
        var mediator = _mediatorMock.Object;
        var documentsApi = _documentsApiMock.Object;
        var userInfo = _userInfoMock.Object;

        Assert.Throws<ArgumentNullException>(() => new GetFilteredDocumentsByTransactionIdQueryHandler(null!, documentsApi, userInfo));
        Assert.Throws<ArgumentNullException>(() => new GetFilteredDocumentsByTransactionIdQueryHandler(mediator, null!, userInfo));
        Assert.Throws<ArgumentNullException>(() => new GetFilteredDocumentsByTransactionIdQueryHandler(mediator, documentsApi, null!));
    }
}
