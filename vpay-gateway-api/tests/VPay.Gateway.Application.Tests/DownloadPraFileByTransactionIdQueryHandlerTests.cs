using System.Net;
using System.Text;
using FluentAssertions;
using FluentAssertions.Execution;
using MediatR;
using Moq;
using Refit;
using VPay.Extensions.Exceptions;
using VPay.Gateway.Common.Documents;
using VPay.Gateway.Common.Kubera;
using Xunit;

namespace VPay.Gateway.Application.Tests;

public class DownloadPraFileByTransactionIdQueryHandlerTests : IDisposable
{
    private readonly Mock<IDocumentsApi> _documentsApiMock = new Mock<IDocumentsApi>(MockBehavior.Strict);
    private readonly Mock<IMediator> _mediatorMock = new Mock<IMediator>(MockBehavior.Strict);
    private readonly Mock<IUserInfo> _userInfoMock = new Mock<IUserInfo>(MockBehavior.Strict);
    private readonly DownloadPraFileByTransactionIdQueryHandler _handler;

    public DownloadPraFileByTransactionIdQueryHandlerTests() =>
        _handler = new DownloadPraFileByTransactionIdQueryHandler(_mediatorMock.Object, _documentsApiMock.Object, _userInfoMock.Object);

    public void Dispose()
    {
        _documentsApiMock.VerifyAll();
        _mediatorMock.VerifyAll();
        _userInfoMock.VerifyAll();
    }

    public class Handle_Unauthorized : DownloadPraFileByTransactionIdQueryHandlerTests
    {
        private readonly DownloadPraFileByTransactionIdQuery _query;

        public Handle_Unauthorized()
        {
            _query = new DownloadPraFileByTransactionIdQuery(100, false);
        }

        [Fact]
        public async Task WhenTheTransactionCannotBeFound_ThenNullIsReturned()
        {
            _mediatorMock
                .Setup(x => x.Send(It.Is<GetTransactionByIdQuery>(q => q.Id == _query.TransactionId), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Common.Kubera.Models.Transaction)null)
                .Verifiable();

            var sut = await _handler.Handle(_query, default).ConfigureAwait(false);

            sut.Should().BeNull();
        }

        [Fact]
        public async Task WhenTheTransactionsClientCodeIsNotOneForTheClient_ThenAnNotAuthorizedExceptionShouldBeThrown()
        {
            var clientCode = "NOTVALID";
            var beCode = "TEST";
            var ptCode = "CLVCP";

            _mediatorMock
                .Setup(x => x.Send(It.Is<GetTransactionByIdQuery>(q => q.Id == _query.TransactionId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Common.Kubera.Models.Transaction { TransactionId = _query.TransactionId, ClientCode = clientCode, BillingEntityCode = beCode, PaymentTypeCode = "CLVCP" })
                .Verifiable();

            _userInfoMock.Setup(x => x.CanAccess(clientCode, beCode, ptCode))
                .Returns(false)
                .Verifiable();

            Func<Task> func = async () => await _handler.Handle(_query, default).ConfigureAwait(false);

            await func.Should().ThrowExactlyAsync<NotAuthorizedException>().ConfigureAwait(false);
        }

        [Theory]
        [InlineData("OPACH")]
        [InlineData("CLNPF")]
        [InlineData("CLCFT")]
        public async Task When_ThePaymentTypeCodeIsValidForTheClient_Then_NoExceptionShouldBeThrown(string paymentType)
        {
            var clientCode = "ISVALID";
            var beCode = "TEST";

            _mediatorMock
                .Setup(x => x.Send(It.Is<GetTransactionByIdQuery>(x => x.Id == _query.TransactionId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Common.Kubera.Models.Transaction { TransactionId = _query.TransactionId, ClientCode = clientCode, BillingEntityCode = beCode, PaymentTypeCode = paymentType })
                .Verifiable();

            _userInfoMock.Setup(x => x.CanAccess(It.IsAny<string>(), It.IsAny<string>(), It.Is<string>(s => (s == "OPACH" || s == "CLNPF" || s == "CLCFT"))))
                .Returns(true)
                .Verifiable();

            Func<Task> func = async () => await _handler.Handle(_query, default).ConfigureAwait(false);

            await func.Should().NotThrowAsync<NotAuthorizedException>().ConfigureAwait(false);
        }

        [Theory]
        [InlineData("CLCHK")]
        [InlineData("CLVCP")]
        [InlineData("CLCCK")]
        public async Task When_ThePaymentTypeCodeIsNotValidForTheClient_Then_NotAuthorizedExceptionShouldBeThrown(string paymentType)
        {
            var clientCode = "ISVALID";
            var beCode = "TEST";

            _mediatorMock
                .Setup(x => x.Send(It.Is<GetTransactionByIdQuery>(x => x.Id == _query.TransactionId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Common.Kubera.Models.Transaction { TransactionId = _query.TransactionId, ClientCode = clientCode, BillingEntityCode = beCode, PaymentTypeCode = paymentType })
                .Verifiable();

            _userInfoMock.Setup(x => x.CanAccess(It.IsAny<string>(), It.IsAny<string>(), It.Is<string>(s => !(s == "OPACH" || s == "CLNPF" || s == "CLCFT"))))
                .Returns(false)
                .Verifiable();

            Func<Task> func = async () => await _handler.Handle(_query, default).ConfigureAwait(false);

            await func.Should().ThrowExactlyAsync<NotAuthorizedException>().ConfigureAwait(false);
        }
    }

    public class Handle_Authorized : DownloadPraFileByTransactionIdQueryHandlerTests
    {
        private readonly DownloadPraFileByTransactionIdQuery _query;
        private readonly Common.Kubera.Models.Transaction _transaction;

        public Handle_Authorized()
        {
            _query = new DownloadPraFileByTransactionIdQuery(100, false);
            _transaction = new Common.Kubera.Models.Transaction { TransactionId = _query.TransactionId, ClientCode = "VPA1", BillingEntityCode = "VPA1-SESR", PaymentTypeCode = "CLVCP", DateLoaded = DateTime.Now };
            _mediatorMock
                .Setup(x => x.Send(It.Is<GetTransactionByIdQuery>(q => q.Id == _query.TransactionId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(_transaction)
                .Verifiable();

            _userInfoMock.Setup(x => x.CanAccess("VPA1", "VPA1-SESR", "CLVCP"))
                .Returns(true)
                .Verifiable();
        }

        [Fact]
        public async Task WhenTheDocumentForTheTransactionIs404NotFoundAndTransactionIsWithin2days_ThenItemNotFoundExceptionIsThrown()
        {
            _transaction.DateLoaded = DateTime.Now.AddDays(-2).AddMinutes(1);
            HttpResponseMessage responseMessage = new HttpResponseMessage(HttpStatusCode.NotFound);
            responseMessage.Content = new StreamContent(new MemoryStream());
            responseMessage.RequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://test.url");

            var apiResponse = new ApiResponse<HttpContent>(responseMessage, responseMessage.Content, new RefitSettings());

            _documentsApiMock.Setup(x => x.DownloadDistributionPacket(_transaction.TransactionId, _transaction.PaymentTypeCode, default))
                .ReturnsAsync(apiResponse).Verifiable();

            Func<Task> func = async () => await _handler.Handle(_query, default).ConfigureAwait(false);

            var value = await func.Should().ThrowExactlyAsync<ItemNotFoundException>().ConfigureAwait(false);
            value.Which.Message.Should().Be("PRA_REQUESTED");
        }

        [Fact]
        public async Task WhenTheDocumentForTheTransactionIs404NotFoundAndTransactionIsOutsideOf2Days_ThenItemNotFoundExceptionIsThrown()
        {
            _transaction.DateLoaded = DateTime.Now.AddDays(-2).AddMinutes(-1);
            HttpResponseMessage responseMessage = new HttpResponseMessage(HttpStatusCode.NotFound);
            responseMessage.Content = new StreamContent(new MemoryStream());
            responseMessage.RequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://test.url");

            var apiResponse = new ApiResponse<HttpContent>(responseMessage, responseMessage.Content, new RefitSettings());

            _documentsApiMock.Setup(x => x.DownloadDistributionPacket(_transaction.TransactionId, _transaction.PaymentTypeCode, default))
                .ReturnsAsync(apiResponse).Verifiable();

            Func<Task> func = async () => await _handler.Handle(_query, default).ConfigureAwait(false);

            var value = await func.Should().ThrowExactlyAsync<ItemNotFoundException>().ConfigureAwait(false);
            value.Which.Message.Should().Be("PRA_NOT_FOUND");
        }

        [Theory]
        [InlineData(HttpStatusCode.BadRequest)]
        [InlineData(HttpStatusCode.Conflict)]
        [InlineData(HttpStatusCode.InternalServerError)]
        public async Task WhenTheDocumentForTheTransactionIsNotSuccessOr404_ThenAnApiExceptionIsThrown(HttpStatusCode statusCode)
        {
            HttpResponseMessage responseMessage = new HttpResponseMessage(statusCode);
            responseMessage.Content = new StreamContent(new MemoryStream());
            responseMessage.RequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://test.url");

            var apiResponse = new ApiResponse<HttpContent>(responseMessage, responseMessage.Content, new RefitSettings());

            _documentsApiMock.Setup(x => x.DownloadDistributionPacket(_transaction.TransactionId, _transaction.PaymentTypeCode, default))
                .ReturnsAsync(apiResponse).Verifiable();

            Func<Task> func = async () => await _handler.Handle(_query, default).ConfigureAwait(false);

            await func.Should().ThrowExactlyAsync<ApiException>().ConfigureAwait(false);
        }

        [Theory]
        [InlineData("", "", "pra-100.pdf", "application/pdf")]
        [InlineData("", "test/pdf", "pra-100.pdf", "test/pdf")]
        [InlineData("12396.pdf", "", "12396.pdf", "application/pdf")]
        [InlineData("123964.txt", "", "123964.txt", "text/plain")]
        [InlineData("123965.pdf", "test/pdf", "123965.pdf", "test/pdf")]
        [InlineData("123967.txt", "text/plain", "123967.txt", "text/plain")]
        [InlineData("123968.pdf", "application/pdf", "123968.pdf", "application/pdf")]
        public async Task WhenTheDocumentIsFound_ThenItWillReturnDocumentWithNameAndMediaType(string setupFileName, string setupMediaType, string expectedFileName, string expectedMediaType)
        {
            await using var stream = new MemoryStream();
            //Arrange
            var setupStreamText = "Test Stream";

            stream.Write(Encoding.Default.GetBytes(setupStreamText), 0, setupStreamText.Length);
            stream.Seek(0, SeekOrigin.Begin);

            HttpResponseMessage responseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            responseMessage.Content = new StreamContent(stream);
            responseMessage.RequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://test.url");

            if (!string.IsNullOrWhiteSpace(setupFileName))
            {
                responseMessage.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment") { FileName = setupFileName };
            }

            if (!string.IsNullOrWhiteSpace(setupMediaType))
            {
                responseMessage.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(setupMediaType);
            }

            var apiResponse = new ApiResponse<HttpContent>(responseMessage, responseMessage.Content, new RefitSettings());

            _documentsApiMock.Setup(x => x.DownloadDistributionPacket(_transaction.TransactionId, _transaction.PaymentTypeCode, default))
                .ReturnsAsync(apiResponse).Verifiable();

            //Act
            var result = await _handler.Handle(_query, default).ConfigureAwait(false);

            //Assert
            StreamReader reader = new StreamReader(result.Stream);
            string text = await reader.ReadToEndAsync().ConfigureAwait((false));

            using (new AssertionScope())
            {
                result.ContentType.Should().Be(expectedMediaType);
                result.FileName.Should().Be(expectedFileName);

                text.Should().Be(setupStreamText);
            }
        }
    }
}
