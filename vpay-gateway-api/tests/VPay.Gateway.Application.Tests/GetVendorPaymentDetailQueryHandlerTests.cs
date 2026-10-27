using System.Globalization;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Vpay.Gateway.Common.Vpay;
using Vpay.Gateway.Common.Vpay.Models;
using VPay.Extensions.Exceptions;
using VPay.Gateway.Application;
using Xunit;

public class GetVendorPaymentDetailQueryHandlerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<IVPayApi> _vpayApiMock;
    private readonly Mock<IUserInfo> _userInfoMock;
    private readonly Mock<ILogger<GetVendorPaymentDetailQueryHandler>> _loggerMock;
    private readonly GetVendorPaymentDetailQueryHandler _handler;

    public GetVendorPaymentDetailQueryHandlerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _vpayApiMock = new Mock<IVPayApi>();
        _userInfoMock = new Mock<IUserInfo>();
        _loggerMock = new Mock<ILogger<GetVendorPaymentDetailQueryHandler>>();

        _handler = new GetVendorPaymentDetailQueryHandler(
            _mediatorMock.Object,
            _vpayApiMock.Object,
            _userInfoMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task Handle_ReturnsTransactionSearchResponseVendor_WhenValidRequest()
    {
        // Arrange
        var transactionId = 123456789L;
        string pretransId = "ZPRE123456";
        var query = new GetVendorPaymentDetailQuery(pretransId, transactionId);

        var transactionDetail = new TransactionSearchResponse
        {
            TransactionType = "CLEFT",
            LoadDate = DateTime.UtcNow,
            GroupNumber = "Group123",
            ClientGroup = "ClientGroup123",
            BankName = "Bank123",
            BillingEntity = new BillingEntity(),
            //ActionList = new List<TransactionEvent>
            //{
            //    new TransactionEvent { Status = "PURCHASE", Timestamp = DateTime.UtcNow }
            //},
            LastUpdated = DateTime.UtcNow,
            AvailableBalance = 1000,
            CurrentBalance = 2000,
            StatusMessage = "Success"
        };

        var transactionChain = new List<ReissueTransactionIDs>
        {
            new ReissueTransactionIDs { TransactionId = "987654321" }
        };

        var transactionCorrespondance = new List<Correspondence>
        {
            new Correspondence { Id = 1, SenderName = "Sender" }
        };

        _vpayApiMock
            .Setup(api => api.GetTransactionDetailsByTransactionID(transactionId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactionDetail);

        _vpayApiMock
            .Setup(api => api.GetTransactionChain(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactionChain);

        _vpayApiMock
            .Setup(api => api.GetTranCorrespondanceDetails(transactionId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactionCorrespondance);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(transactionId, result.vpayTransactionID);
        Assert.Equal(pretransId, result.PreTransID);
        Assert.Equal(transactionDetail.AvailableBalance, result.AvailableBalance);
        Assert.Equal(transactionChain.Count, result.TransactionChainResponseInternal.Count);
    }

    [Fact]
    public async Task Handle_ThrowsNotFoundException_WhenTransactionNotFound()
    {
        // Arrange
        var transactionId = 123456789L;
        string pretransId = "PRE123456";
        var query = new GetVendorPaymentDetailQuery(pretransId, transactionId);
        _vpayApiMock
            .Setup(api => api.GetTransactionDetailsByTransactionID(transactionId, true, It.IsAny<CancellationToken>()))
            .ThrowsAsync(await Refit.ApiException.Create(
                new HttpRequestMessage(HttpMethod.Get, "http://localhost"),
                HttpMethod.Get,
                new HttpResponseMessage(HttpStatusCode.NotFound),
                null));
        // Act & Assert
        await Assert.ThrowsAsync<ItemNotFoundException>(() => _handler.Handle(query, CancellationToken.None));
    }


    [Fact]
    public async Task Handle_WhenPretransactionIdIsEmpty_ResolvesPreTransIdFromFirstValidTransactionChainEntry()
    {
        var transactionId = 123456789L;
        var query = new GetVendorPaymentDetailQuery(string.Empty, transactionId);

        var transactionDetail = CreateTransactionDetail();
        var transactionChain = new List<ReissueTransactionIDs>
        {
            new() { TransactionId = transactionId.ToString() },
            new() { TransactionId = "987654321" },
            new() { TransactionId = "not-a-number" }
        };
        var transactionCorrespondance = new List<Correspondence>
        {
            new() { Id = 1, SenderName = "Sender" }
        };

        _vpayApiMock
            .Setup(api => api.GetTransactionDetailsByTransactionID(transactionId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactionDetail);

        _vpayApiMock
            .Setup(api => api.GetTransactionChain(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactionChain);

        _vpayApiMock
            .Setup(api => api.GetTranCorrespondanceDetails(transactionId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactionCorrespondance);

        _mediatorMock
            .Setup(m => m.Send(It.Is<GetPreTransIdbyTransactionQuery>(q => q.TransactionId == 987654321L), It.IsAny<CancellationToken>()))
            .ReturnsAsync("123456");

        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Z123456", result.PreTransID);

        _mediatorMock.Verify(
            m => m.Send(It.Is<GetPreTransIdbyTransactionQuery>(q => q.TransactionId == 987654321L), It.IsAny<CancellationToken>()),
            Times.Once);

        _mediatorMock.Verify(
            m => m.Send(It.Is<GetPreTransIdbyTransactionQuery>(q => q.TransactionId == transactionId), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenPretransactionIdIsZero_StopsOnFirstResolvablePreTransIdAndPreservesExistingZPrefix()
    {
        var transactionId = 123456789L;
        var query = new GetVendorPaymentDetailQuery("0", transactionId);

        var transactionDetail = CreateTransactionDetail();
        var transactionChain = new List<ReissueTransactionIDs>
        {
            new() { TransactionId = "987654321" },
            new() { TransactionId = "555555555" }
        };
        var transactionCorrespondance = new List<Correspondence>
        {
            new() { Id = 1, SenderName = "Sender" }
        };

        _vpayApiMock
            .Setup(api => api.GetTransactionDetailsByTransactionID(transactionId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactionDetail);

        _vpayApiMock
            .Setup(api => api.GetTransactionChain(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactionChain);

        _vpayApiMock
            .Setup(api => api.GetTranCorrespondanceDetails(transactionId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactionCorrespondance);

        _mediatorMock
            .Setup(m => m.Send(It.Is<GetPreTransIdbyTransactionQuery>(q => q.TransactionId == 987654321L), It.IsAny<CancellationToken>()))
            .ReturnsAsync("z999999");

        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("z999999", result.PreTransID);

        _mediatorMock.Verify(
            m => m.Send(It.Is<GetPreTransIdbyTransactionQuery>(q => q.TransactionId == 987654321L), It.IsAny<CancellationToken>()),
            Times.Once);

        _mediatorMock.Verify(
            m => m.Send(It.Is<GetPreTransIdbyTransactionQuery>(q => q.TransactionId == 555555555L), It.IsAny<CancellationToken>()),
            Times.Never);
    }



    [Fact]
    public async Task Handle_ThrowsException_WhenUnexpectedErrorOccurs()
    {
        var transactionId = 123456789L;
        string pretransId = "PRE123456";
        var query = new GetVendorPaymentDetailQuery(pretransId, transactionId);

        _vpayApiMock
            .Setup(api => api.GetTransactionDetailsByTransactionID(transactionId, true, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Unexpected error"));

        await Assert.ThrowsAsync<Exception>(() => _handler.Handle(query, CancellationToken.None));
    }

    private static TransactionSearchResponse CreateTransactionDetail()
    {
        return new TransactionSearchResponse
        {
            TransactionType = "CLEFT",
            LoadDate = DateTime.UtcNow,
            GroupNumber = "Group123",
            BankName = "Bank123",
            BillingEntity = new BillingEntity
            {
                Code = "BE1",
                Tpa = new Tpa
                {
                    Code = "CLIENT1"
                }
            },
            Events = new List<TransactionEvent>
            {
                new()
                {
                    Status = "PURCHASE",
                    Timestamp = DateTime.UtcNow.AddMinutes(-1)
                },
                new()
                {
                    Status = "LOADED",
                    Timestamp = DateTime.UtcNow.AddMinutes(-2)
                }
            },
            AvailableBalance = 1000,
            CurrentBalance = 2000,
            Amount = 100
        };
    }

    [Fact]
    public async Task Handle_WhenPretransactionIdIsEmpty_ResolvesPreTransIdFromTransactionChain()
    {
        // Arrange
        const long requestTransactionId = 123456789L;
        var query = new GetVendorPaymentDetailQuery(string.Empty, requestTransactionId);

        var transactionDetail = CreateTransactionDetail();
        var transactionChain = new List<ReissueTransactionIDs>
        {
            new() { TransactionId = requestTransactionId.ToString(CultureInfo.InvariantCulture) },
            new() { TransactionId = "987654321" }
        };
        var transactionCorrespondance = new List<Correspondence>
        {
            new() { Id = 1, SenderName = "Sender" }
        };

        _vpayApiMock
            .Setup(api => api.GetTransactionDetailsByTransactionID(requestTransactionId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactionDetail);

        _vpayApiMock
            .Setup(api => api.GetTransactionChain(requestTransactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactionChain);

        _vpayApiMock
            .Setup(api => api.GetTranCorrespondanceDetails(requestTransactionId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactionCorrespondance);

        _mediatorMock
            .Setup(m => m.Send(
                It.Is<GetPreTransIdbyTransactionQuery>(q => q.TransactionId == 987654321L),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("123456");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Z123456", result.PreTransID);

        _mediatorMock.Verify(
            m => m.Send(
                It.Is<GetPreTransIdbyTransactionQuery>(q => q.TransactionId == 987654321L),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mediatorMock.Verify(
            m => m.Send(
                It.Is<GetPreTransIdbyTransactionQuery>(q => q.TransactionId == requestTransactionId),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenPretransactionIdIsZero_StopsAtFirstResolvableTransactionChainEntry()
    {
        // Arrange
        const long requestTransactionId = 123456789L;
        var query = new GetVendorPaymentDetailQuery("0", requestTransactionId);

        var transactionDetail = CreateTransactionDetail();
        var transactionChain = new List<ReissueTransactionIDs>
        {
            new() { TransactionId = "987654321" },
            new() { TransactionId = "555555555" }
        };
        var transactionCorrespondance = new List<Correspondence>
        {
            new() { Id = 1, SenderName = "Sender" }
        };

        _vpayApiMock
            .Setup(api => api.GetTransactionDetailsByTransactionID(requestTransactionId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactionDetail);

        _vpayApiMock
            .Setup(api => api.GetTransactionChain(requestTransactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactionChain);

        _vpayApiMock
            .Setup(api => api.GetTranCorrespondanceDetails(requestTransactionId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactionCorrespondance);

        _mediatorMock
            .Setup(m => m.Send(
                It.Is<GetPreTransIdbyTransactionQuery>(q => q.TransactionId == 987654321L),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("Z999999");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Z999999", result.PreTransID);

        _mediatorMock.Verify(
            m => m.Send(
                It.Is<GetPreTransIdbyTransactionQuery>(q => q.TransactionId == 987654321L),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mediatorMock.Verify(
            m => m.Send(
                It.Is<GetPreTransIdbyTransactionQuery>(q => q.TransactionId == 555555555L),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

}
