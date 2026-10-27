using System.Net;
using FluentAssertions;
using Moq;
using Refit;

namespace VPay.Gateway.Common.RestPayments.Tests;

public class RejectStpPaymentTests
{
    private readonly IRestPaymentApi _restPaymentApi;
    private readonly RejectStpPayment.Handler _handler;

    private const long TransactionId = 1;
    private const string NetworkPartner = "MY_NETWORK";
    private const string RejectionCode = "UTLA";
    private const string RejectionReason = "Unable to Locate Account";
    private const string ResponseMessage = "Do STP DTP Completion";

    public RejectStpPaymentTests()
    {
        _restPaymentApi = Mock.Of<IRestPaymentApi>(MockBehavior.Strict);
        _handler = new RejectStpPayment.Handler(_restPaymentApi);
    }

    public class Constructor : RejectStpPaymentTests
    {
        [Fact]
        public void WhenMediatorIsNull_ThrowsArgumentNullException()
        {
            Action act = new(() =>
            {
                var _ = new RejectStpPayment.Handler(null!);
            });
            act.Should().ThrowExactly<ArgumentNullException>()
                .Which.ParamName.Should().Be("restPaymentApi");
        }
    }

    public class Handle : RejectStpPaymentTests
    {
        [Fact]
        public async Task WhenRequestIsNull_ThrowsArgumentNullException()
        {
            Func<Task> act = async () => await _handler.Handle(null!, CancellationToken.None);
            var result = await act.Should().ThrowAsync<ArgumentNullException>();
            result.Which.ParamName.Should().Be("request");
        }

        [Fact]
        public async Task WhenRequestIsValid_ReturnsResult()
        {
            var requestItem = new RejectStpPayment.Request(NetworkPartner, TransactionId, RejectionCode, RejectionReason);
            var responseItem = new RejectStpPayment.Response(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, ResponseMessage);

            var responseMock = new Mock<IApiResponse<RejectStpPayment.Response>>();
            responseMock.Setup(x => x.Content).Returns(responseItem);
            responseMock.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);

            var restPaymentApiMock = new Mock<IRestPaymentApi>();
            restPaymentApiMock
                .Setup(x => x.RejectStpPayment
                (
                    It.Is<string>(x => x == NetworkPartner),
                    It.Is<long>(x => x == TransactionId),
                    It.Is<string>(x => x == RejectionCode),
                    It.Is<string>(x => x == RejectionReason),
                    It.IsAny<CancellationToken>())
                )
                .ReturnsAsync(responseMock.Object);

            var handler = new RejectStpPayment.Handler(restPaymentApiMock.Object);

            var response = await handler.Handle(requestItem, CancellationToken.None)
                .ConfigureAwait(false);

            response.Should().NotBeNull();
        }
    }
}
