using System.Net;
using AutoFixture;
using FluentAssertions;
using Moq;
using Refit;

namespace VPay.Gateway.Common.RestPayments.Tests;

public class TransactionInfoTests
{
#pragma warning disable CA1806
    [Fact]
    public void NullAPI_ThrowsException() =>
        new Action(() => new TransactionInfo.Handler(null!))
            .Should().ThrowExactly<ArgumentNullException>()
            .And.ParamName.Should().Be("restPaymentApi");
#pragma warning restore CA1806

    [Fact]
    public async Task Handler_ReturnsAsExpected()
    {
        var fixture = new Fixture();
        var responseMock = new Mock<IApiResponse<TransactionInfo.Response>>();
        responseMock.Setup(x => x.Content).Returns(fixture.Build<TransactionInfo.Response>().Create());
        responseMock.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);

        var restPaymentApiMock = new Mock<IRestPaymentApi>();
        restPaymentApiMock
            .Setup(x => x.TransactionInfo(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseMock.Object);

        var handler = new TransactionInfo.Handler(restPaymentApiMock.Object);
        var response = await handler.Handle(new TransactionInfo.Query(112344), CancellationToken.None)
            .ConfigureAwait(false);

        response.Should().NotBeNull();
    }
}
