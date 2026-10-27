using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using FluentAssertions.Execution;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Newtonsoft.Json;
using Refit;
using VPay.Extensions.Exceptions;
using VPay.Gateway.Common.Documents;
using VPay.Gateway.Common.Documents.Models;
using VPay.Gateway.Common.Kubera;
using Xunit;

namespace VPay.Gateway.Application.Tests;

public class Download835FileByTransactionIdQueryTests : IDisposable
{
    private readonly Mock<IDocumentsApi> _documentsApiMock = new(MockBehavior.Strict);
    private readonly Mock<IMediator> _mediatorMock = new(MockBehavior.Strict);
    private readonly Mock<IUserInfo> _userInfoMock = new(MockBehavior.Strict);
    private readonly ILogger<Download835FileByTransactionIdQueryHandler> _logger = NullLogger<Download835FileByTransactionIdQueryHandler>.Instance;
    private readonly Download835FileByTransactionIdQueryHandler _handler;

    public Download835FileByTransactionIdQueryTests() =>
        _handler = new Download835FileByTransactionIdQueryHandler(_mediatorMock.Object, _documentsApiMock.Object, _userInfoMock.Object, _logger);

    public void Dispose()
    {
        _documentsApiMock.VerifyAll();
        _mediatorMock.VerifyAll();
        _userInfoMock.VerifyAll();

        GC.SuppressFinalize(this);
    }

    public class Handle : Download835FileByTransactionIdQueryTests
    {
        private readonly HttpRequestMessage _httpRequest = new(HttpMethod.Get, "http://test.url");
        private readonly HttpMethod _httpMethod = HttpMethod.Get;

        private HttpResponseMessage _httpResponseNotFound = new(HttpStatusCode.NotFound);
        private readonly HttpResponseMessage _httpResponseConflict = new(HttpStatusCode.Conflict);
        private readonly HttpResponseMessage _httpResponseServerError = new(HttpStatusCode.InternalServerError);

        [Fact]
        public async Task Handle_ThrowsNotAuthorized_WhenUserCannotAccessPayment()
        {
            // Arrange
            _userInfoMock.Setup(x => x.IsInRoleWithRestrictedPaymentTypes()).Returns(false);
            _mediatorMock.Setup(x => x.Send(It.IsAny<GetDocumentPaymentDetailByTransactionIDQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TransactionPayment { ClientCode = "C", BillingEntityCode = "B" });

            _userInfoMock.Setup(x => x.CanAccess("C", "B", It.IsAny<string>())).Returns(false);

            // Act & Assert
            await Assert.ThrowsAsync<NotAuthorizedException>(() =>
                _handler.Handle(new Download835FileByTransactionIdQuery(1), CancellationToken.None));
        }

        [Fact]
        public async Task Handle_ThrowsItemNotFound_WhenTransactionNotFoundAnywhere()
        {
            // Arrange
            _userInfoMock.Setup(x => x.IsInRoleWithRestrictedPaymentTypes()).Returns(false);
            _mediatorMock.Setup(x => x.Send(It.IsAny<GetDocumentPaymentDetailByTransactionIDQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((TransactionPayment)null);

            _mediatorMock.Setup(x => x.Send(It.IsAny<GetTransactionByIdQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Common.Kubera.Models.Transaction)null);

            // Act & Assert
            await Assert.ThrowsAsync<ItemNotFoundException>(() =>
                _handler.Handle(new Download835FileByTransactionIdQuery(1), CancellationToken.None));
        }

        [Fact]
        public async Task Handle_ThrowsNotAuthorized_WhenUserCannotAccessFallbackTransaction()
        {
            // Arrange
            _userInfoMock.Setup(x => x.IsInRoleWithRestrictedPaymentTypes()).Returns(false);
            _mediatorMock.Setup(x => x.Send(It.IsAny<GetDocumentPaymentDetailByTransactionIDQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((TransactionPayment)null);

            var trans = new Common.Kubera.Models.Transaction { ClientCode = "C", BillingEntityCode = "B", PaymentTypeCode = "P" };
            _mediatorMock.Setup(x => x.Send(It.IsAny<GetTransactionByIdQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(trans);
            _userInfoMock.Setup(x => x.CanAccess("C", "B", "P")).Returns(false);

            // Act & Assert
            await Assert.ThrowsAsync<NotAuthorizedException>(() =>
                _handler.Handle(new Download835FileByTransactionIdQuery(1), CancellationToken.None));
        }

        [Fact]
        public async Task Handle_ThrowsItemNotFound_OnApiExceptionConflict()
        {
            // Arrange
            SetupUserAndTransaction();

            var apiException = await ApiException.Create(_httpRequest, _httpMethod, _httpResponseConflict, null, null);

            _documentsApiMock.Setup(x => x.Download835(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(apiException);

            // Act & Assert
            await Assert.ThrowsAsync<ItemNotFoundException>(() =>
                _handler.Handle(new Download835FileByTransactionIdQuery(1), CancellationToken.None));
        }

        [Fact]
        public async Task Handle_ThrowsApiException_OnApiExceptionNotFound_WithNullProblemDetails()
        {
            // Arrange
            SetupUserAndTransaction();

            var apiException = await ApiException.Create(_httpRequest, _httpMethod, new HttpResponseMessage(HttpStatusCode.NotFound), null, null);

            _documentsApiMock.Setup(x => x.Download835(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(apiException);

            // Simulate GetContentAsAsync<ProblemDetails>() returning null
            apiException.Data["GetContentAsAsync<ProblemDetails>"] = null;

            // Act & Assert
            await Assert.ThrowsAsync<ApiException>(() =>
                _handler.Handle(new Download835FileByTransactionIdQuery(1), CancellationToken.None));
        }

        [Fact]
        public async Task Handle_ThrowsItemNotFound_OnApiExceptionNotFound_WithTransactionMissingDetail()
        {
            // Arrange
            SetupUserAndTransaction();

            var problemDetails = new ProblemDetails { Detail = Edi835Statuses.TransactionMissing };
            var jsonContent = JsonContent.Create(problemDetails, new MediaTypeHeaderValue("application/problem+json"));
            _httpResponseNotFound = new(HttpStatusCode.NotFound) { Content = jsonContent };

            var apiException = await ApiException.Create(_httpRequest, _httpMethod, _httpResponseNotFound, new RefitSettings(), null);

            _documentsApiMock.Setup(x => x.Download835(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(apiException);

            apiException.Data["GetContentAsAsync<ProblemDetails>"] = problemDetails;


            // Act & Assert
            await Assert.ThrowsAsync<ItemNotFoundException>(() =>
                _handler.Handle(new Download835FileByTransactionIdQuery(1), CancellationToken.None));
        }

        [Fact]
        public async Task Handle_ThrowsItemNotFound_OnApiExceptionNotFound_WithNotNeededFromDocSysDetail()
        {
            // Arrange
            SetupUserAndTransaction();

            var problemDetails = new ProblemDetails { Detail = Edi835Statuses.NotNeededFromDocSys };
            var jsonContent = JsonContent.Create(problemDetails, new MediaTypeHeaderValue("application/problem+json"));
            _httpResponseNotFound = new(HttpStatusCode.NotFound) { Content = jsonContent };

            var apiException = await ApiException.Create(_httpRequest, _httpMethod, _httpResponseNotFound, new RefitSettings(), null);

            _documentsApiMock.Setup(x => x.Download835(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(apiException);

            // Act & Assert
            await Assert.ThrowsAsync<ItemNotFoundException>(() =>
                _handler.Handle(new Download835FileByTransactionIdQuery(1), CancellationToken.None));
        }

        [Fact]
        public async Task Handle_ThrowsItemNotFound_OnApiExceptionNotFound_WithOtherDetail()
        {
            // Arrange
            SetupUserAndTransaction();

            var stringContent = new StringContent(JsonConvert.SerializeObject(new ProblemDetails { Status = (int)HttpStatusCode.NotFound, Detail = "Some error occurred" }));
            _httpResponseNotFound = new(HttpStatusCode.NotFound) { Content = stringContent };

            var apiException = await ApiException.Create(_httpRequest, _httpMethod, _httpResponseNotFound, new RefitSettings(), null);

            _documentsApiMock.Setup(x => x.Download835(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(apiException);

            // Act & Assert
            await Assert.ThrowsAsync<ItemNotFoundException>(() =>
                _handler.Handle(new Download835FileByTransactionIdQuery(1), CancellationToken.None));
        }

        [Fact]
        public async Task Handle_Throws_OnUnexpectedApiException()
        {
            // Arrange
            SetupUserAndTransaction();
            var apiException = await ApiException.Create(_httpRequest, _httpMethod, _httpResponseServerError, null, null);
            _documentsApiMock.Setup(x => x.Download835(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(apiException);

            // Act & Assert
            await Assert.ThrowsAsync<ApiException>(() =>
                _handler.Handle(new Download835FileByTransactionIdQuery(1), CancellationToken.None));
        }

        private void SetupUserAndTransaction()
        {
            _userInfoMock.Setup(x => x.IsInRoleWithRestrictedPaymentTypes()).Returns(false);
            _mediatorMock.Setup(x => x.Send(It.IsAny<GetDocumentPaymentDetailByTransactionIDQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((TransactionPayment)null);
            _mediatorMock.Setup(x => x.Send(It.IsAny<GetTransactionByIdQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Common.Kubera.Models.Transaction { ClientCode = "C", BillingEntityCode = "B", PaymentTypeCode = "P", DateLoaded = DateTime.UtcNow });
            _userInfoMock.Setup(x => x.CanAccess("C", "B", "P")).Returns(true);
        }
    }

    public class Handle_Unauthorized : Download835FileByTransactionIdQueryTests
    {
        private readonly Download835FileByTransactionIdQuery _query;

        public Handle_Unauthorized()
        {
            _query = new Download835FileByTransactionIdQuery(100);
        }

        [Fact]
        public async Task WhenTheTransactionCannotBeFoundAndUserInRoleWithRestrictedPaymentTypes_ThrowsItemNotFoundException()
        {
            _mediatorMock
                .Setup(x => x.Send(It.Is<GetTransactionByIdQuery>(q => q.Id == _query.TransactionId), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Common.Kubera.Models.Transaction)null)
                .Verifiable();

            _userInfoMock
                .Setup(x => x.IsInRoleWithRestrictedPaymentTypes())
                .Returns(true)
                .Verifiable();

            Func<Task> func = async () => await _handler.Handle(_query, default).ConfigureAwait(false);

            await func.Should().ThrowExactlyAsync<ItemNotFoundException>().ConfigureAwait(false);
        }

        [Fact]
        public async Task WhenTheTransactionCannotBeFoundAndUserNotInRoleWithRestrictedPaymentTypes_ThrowsItemNotFoundException()
        {
            _mediatorMock
                .Setup(x => x.Send(It.Is<GetTransactionByIdQuery>(q => q.Id == _query.TransactionId), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Common.Kubera.Models.Transaction)null)
                .Verifiable();

            _mediatorMock
                .Setup(x => x.Send(It.Is<GetDocumentPaymentDetailByTransactionIDQuery>(q => q.TransactionId == _query.TransactionId), It.IsAny<CancellationToken>()))
                .ReturnsAsync((TransactionPayment)null)
                .Verifiable();

            _userInfoMock
                .Setup(x => x.IsInRoleWithRestrictedPaymentTypes())
                .Returns(false)
                .Verifiable();

            Func<Task> func = async () => await _handler.Handle(_query, default).ConfigureAwait(false);

            await func.Should().ThrowExactlyAsync<ItemNotFoundException>().ConfigureAwait(false);
        }

        [Fact]
        public async Task WhenTheTransactionsClientCodeIsNotOneForTheClient_ThenAnNotAuthorizedExceptionShouldBeThrown()
        {
            const string ClientCode = "NOTVALID";
            const string BeCode = "TEST";
            const string PtCode = "CLVCP";

            _mediatorMock
                .Setup(x => x.Send(It.Is<GetTransactionByIdQuery>(q => q.Id == _query.TransactionId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Common.Kubera.Models.Transaction { TransactionId = _query.TransactionId, ClientCode = ClientCode, BillingEntityCode = BeCode, PaymentTypeCode = PtCode })
                .Verifiable();

            _userInfoMock
                .Setup(x => x.IsInRoleWithRestrictedPaymentTypes())
                .Returns(false)
                .Verifiable();

            _userInfoMock.Setup(x => x.CanAccess(ClientCode, BeCode, PtCode))
                .Returns(false)
                .Verifiable();

            Func<Task> func = async () => await _handler.Handle(_query, default).ConfigureAwait(false);

            await func.Should().ThrowExactlyAsync<NotAuthorizedException>().ConfigureAwait(false);
        }
    }

    public class Handle_Authorized : Download835FileByTransactionIdQueryTests
    {
        private readonly Download835FileByTransactionIdQuery _query;
        private readonly Common.Kubera.Models.Transaction _transaction;

        public Handle_Authorized()
        {
            _query = new Download835FileByTransactionIdQuery(100);
            _transaction = new Common.Kubera.Models.Transaction { TransactionId = _query.TransactionId, ClientCode = "VPA1", BillingEntityCode = "VPA1-SESR", PaymentTypeCode = "CLVCP", DateLoaded = DateTime.Now };
            _mediatorMock
                .Setup(x => x.Send(It.Is<GetTransactionByIdQuery>(q => q.Id == _query.TransactionId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(_transaction)
                .Verifiable();

            _userInfoMock.Setup(x => x.CanAccess("VPA1", "VPA1-SESR", "CLVCP"))
                .Returns(true)
                .Verifiable();
        }

        [Theory]
        [InlineData(HttpStatusCode.BadRequest)]
        [InlineData(HttpStatusCode.InternalServerError)]
        public async Task WhenTheDocumentForTheTransactionIsNotSuccessOr404_ThenAnApiExceptionIsThrown(HttpStatusCode statusCode)
        {
            var responseMessage = new HttpResponseMessage(statusCode)
            {
                Content = new StreamContent(new MemoryStream()),
                RequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://test.url")
            };

            var apiResponse = new ApiResponse<HttpContent>(responseMessage, responseMessage.Content, new RefitSettings());

            var exception = await ApiException.Create(responseMessage.RequestMessage, responseMessage.RequestMessage.Method, responseMessage, new RefitSettings());

            _userInfoMock
                .Setup(x => x.IsInRoleWithRestrictedPaymentTypes())
                .Returns(false)
                .Verifiable();

            _documentsApiMock.Setup(x => x.Download835(_transaction.TransactionId, default))
                .ThrowsAsync(exception).Verifiable();

            Func<Task> func = async () => await _handler.Handle(_query, default).ConfigureAwait(false);

            await func.Should().ThrowExactlyAsync<ApiException>().ConfigureAwait(false);
        }

        [Fact]
        public async Task WhenTheDocumentForTheTransactionIs409Conflict_ThenItemNotFoundExceptionIsThrownWithCorrectMessage()
        {
            var responseMessage = new HttpResponseMessage(HttpStatusCode.Conflict)
            {
                Content = new StreamContent(new MemoryStream()),
                RequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://test.url")
            };

            var apiResponse = new ApiResponse<HttpContent>(responseMessage, responseMessage.Content, new RefitSettings());

            var exception = await ApiException.Create(responseMessage.RequestMessage, responseMessage.RequestMessage.Method, responseMessage, new RefitSettings());

            _userInfoMock
                .Setup(x => x.IsInRoleWithRestrictedPaymentTypes())
                .Returns(false)
                .Verifiable();

            _documentsApiMock.Setup(x => x.Download835(_transaction.TransactionId, default))
                .ThrowsAsync(exception).Verifiable();

            Func<Task> func = async () => await _handler.Handle(_query, default).ConfigureAwait(false);

            var value = await func.Should().ThrowExactlyAsync<ItemNotFoundException>().ConfigureAwait(false);
            value.Which.Message.Should().Be("TRANSACTION_NOT_FOUND");
        }

        [Fact]
        public async Task WhenTheDocumentForTheTransactionIs404ConflictWith835PendingAndTransactionIsMoreThan2daysOld_ThenItemNotFoundExceptionIsThrownWithNotFoundError()
        {
            _transaction.DateLoaded = DateTime.Now.AddDays(-2).AddMilliseconds(-1);
            var responseMessage = new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent(JsonConvert.SerializeObject(new ProblemDetails { Status = (int)HttpStatusCode.NotFound, Detail = "835_PENDING_Parsing" })),
                RequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://test.url")
            };

            var apiResponse = new ApiResponse<HttpContent>(responseMessage, responseMessage.Content, new RefitSettings());

            var exception = await ApiException.Create(responseMessage.RequestMessage, responseMessage.RequestMessage.Method, responseMessage, new RefitSettings());

            _userInfoMock
                .Setup(x => x.IsInRoleWithRestrictedPaymentTypes())
                .Returns(false)
                .Verifiable();

            _documentsApiMock.Setup(x => x.Download835(_transaction.TransactionId, default))
                .ThrowsAsync(exception).Verifiable();

            Func<Task> func = async () => await _handler.Handle(_query, default).ConfigureAwait(false);

            var value = await func.Should().ThrowExactlyAsync<ItemNotFoundException>().ConfigureAwait(false);
            value.Which.Message.Should().Be("835_NOT_FOUND");
        }

        [Theory]
        [InlineData("TRANSACTION_NOT_FOUND", "TRANSACTION_NOT_FOUND")]
        [InlineData("835_NOT_NEEDED", "835_NOT_REQUESTED")]
        [InlineData("835_PENDING_Parsing", "835_REQUESTED")]
        public async Task WhenTheDocumentForTheTransactionIs404Conflict_ThenItemNotFoundExceptionIsThrownWithCorrectMessage(string problemDetail, string expectedMessage)
        {
            var responseMessage = new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent(JsonConvert.SerializeObject(new ProblemDetails { Status = (int)HttpStatusCode.NotFound, Detail = problemDetail })),
                RequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://test.url")
            };

            var apiResponse = new ApiResponse<HttpContent>(responseMessage, responseMessage.Content, new RefitSettings());

            var exception = await ApiException.Create(responseMessage.RequestMessage, responseMessage.RequestMessage.Method, responseMessage, new RefitSettings());

            _userInfoMock
                .Setup(x => x.IsInRoleWithRestrictedPaymentTypes())
                .Returns(false)
                .Verifiable();

            _documentsApiMock.Setup(x => x.Download835(_transaction.TransactionId, default))
                .ThrowsAsync(exception).Verifiable();

            Func<Task> func = async () => await _handler.Handle(_query, default).ConfigureAwait(false);

            var value = await func.Should().ThrowExactlyAsync<ItemNotFoundException>().ConfigureAwait(false);
            value.Which.Message.Should().Be(expectedMessage);
        }

        [Theory]
        [InlineData("", "", "edi-100.835", "text/plain")]
        [InlineData("", "test/plain", "edi-100.835", "test/plain")]
        [InlineData("12396.835", "", "12396.835", "text/plain")]
        [InlineData("123967.835", "text/plain", "123967.835", "text/plain")]
        public async Task WhenTheDocumentIsFound_ThenItWillReturnDocumentWithNameAndMediaType(string setupFileName, string setupMediaType, string expectedFileName, string expectedMediaType)
        {
            await using var stream = new MemoryStream();

            //Arrange
            const string SetupStreamText = "Test Stream";

            stream.Write(Encoding.Default.GetBytes(SetupStreamText), 0, SetupStreamText.Length);
            stream.Seek(0, SeekOrigin.Begin);

            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(stream),
                RequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://test.url")
            };

            if (!string.IsNullOrWhiteSpace(setupFileName))
            {
                responseMessage.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment") { FileName = setupFileName };
            }

            if (!string.IsNullOrWhiteSpace(setupMediaType))
            {
                responseMessage.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(setupMediaType);
            }

            var apiResponse = new ApiResponse<HttpContent>(responseMessage, responseMessage.Content, new RefitSettings());

            _userInfoMock
                .Setup(x => x.IsInRoleWithRestrictedPaymentTypes())
                .Returns(false)
                .Verifiable();

            _documentsApiMock.Setup(x => x.Download835(_transaction.TransactionId, default))
                .ReturnsAsync(apiResponse.Content).Verifiable();

            //Act
            var result = await _handler.Handle(_query, default).ConfigureAwait(false);

            //Assert
            var reader = new StreamReader(result.Stream);
            string text = await reader.ReadToEndAsync().ConfigureAwait(false);

            using (new AssertionScope())
            {
                result.ContentType.Should().Be(expectedMediaType);
                result.FileName.Should().Be(expectedFileName);

                text.Should().Be(SetupStreamText);
            }
        }
    }

    public class Handle_AuthorizedWhenKuberaFails : Download835FileByTransactionIdQueryTests
    {
        private readonly Download835FileByTransactionIdQuery _query;
        private readonly Common.Kubera.Models.Transaction _transaction;

        public Handle_AuthorizedWhenKuberaFails()
        {
            _query = new Download835FileByTransactionIdQuery(100);
            _transaction = new Common.Kubera.Models.Transaction
            {
                TransactionId = _query.TransactionId,
                ClientCode = "VPA1",
                BillingEntityCode = "VPA1-SESR",
                PaymentTypeCode = "CLVCP",
                DateLoaded = new DateTime(2020, 1, 1)
            };
        }

        [Theory]
        [InlineData(HttpStatusCode.BadRequest)]
        [InlineData(HttpStatusCode.Conflict)]
        [InlineData(HttpStatusCode.InternalServerError)]
        public async Task WhenTheDocumentIsNotFoundInKuberaAndApiCallFailsInDocumentApi_ItemNotFoundExceptionIsThrown(HttpStatusCode statusCode)
        {
            _mediatorMock
                .Setup(x => x.Send(It.Is<GetTransactionByIdQuery>(q => q.Id == _query.TransactionId), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Common.Kubera.Models.Transaction)null)
                .Verifiable();

            _mediatorMock
                .Setup(x => x.Send(It.Is<GetDocumentPaymentDetailByTransactionIDQuery>(q => q.TransactionId == _query.TransactionId), It.IsAny<CancellationToken>()))
                .ReturnsAsync((TransactionPayment)null)
                .Verifiable();

            _userInfoMock
                .Setup(x => x.IsInRoleWithRestrictedPaymentTypes())
                .Returns(false)
                .Verifiable();

            var responseMessage = new HttpResponseMessage(statusCode)
            {
                Content = new StreamContent(new MemoryStream()),
                RequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://test.url")
            };

            var apiResponse = new ApiResponse<HttpContent>(responseMessage, responseMessage.Content, new RefitSettings());

            var exception = await ApiException.Create(responseMessage.RequestMessage, responseMessage.RequestMessage.Method, responseMessage, new RefitSettings());

            _mediatorMock
                .Setup(x => x.Send(It.Is<GetDocumentPaymentDetailByTransactionIDQuery>(q => q.TransactionId == _query.TransactionId), It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception).Verifiable();

            Func<Task> func = async () => await _handler.Handle(_query, default).ConfigureAwait(false);

            await func.Should().ThrowExactlyAsync<ItemNotFoundException>().ConfigureAwait(false);
        }

        [Theory]
        [InlineData("", "", "edi-100.835", "text/plain")]
        [InlineData("", "test/plain", "edi-100.835", "test/plain")]
        [InlineData("12396.835", "", "12396.835", "text/plain")]
        [InlineData("123967.835", "text/plain", "123967.835", "text/plain")]
        public async Task WhenDocumentIsFoundInDocumentApi_ThenWillReturnDocument(string setupFileName, string setupMediaType, string expectedFileName, string expectedMediaType)
        {
            //Arrange
            var payment = new TransactionPayment()
            {
                ClientCode = "VPA1",
                BillingEntityCode = "VPA1-SESR",
                TransactionId = _query.TransactionId,
            };

            _mediatorMock
                .Setup(x => x.Send(It.Is<GetDocumentPaymentDetailByTransactionIDQuery>(q => q.TransactionId == _query.TransactionId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(payment)
                .Verifiable();

            _userInfoMock
                .Setup(x => x.IsInRoleWithRestrictedPaymentTypes())
                .Returns(false)
                .Verifiable();

            _userInfoMock.Setup(x => x.CanAccess("VPA1", "VPA1-SESR", ""))
                .Returns(true)
                .Verifiable();

            await using var stream = new MemoryStream();
            const string SetupStreamText = "Test Stream";

            stream.Write(Encoding.Default.GetBytes(SetupStreamText), 0, SetupStreamText.Length);
            stream.Seek(0, SeekOrigin.Begin);

            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(stream),
                RequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://test.url")
            };

            if (!string.IsNullOrWhiteSpace(setupFileName))
            {
                responseMessage.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment") { FileName = setupFileName };
            }

            if (!string.IsNullOrWhiteSpace(setupMediaType))
            {
                responseMessage.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(setupMediaType);
            }

            var apiResponse = new ApiResponse<HttpContent>(responseMessage, responseMessage.Content, new RefitSettings());

            _documentsApiMock.Setup(x => x.Download835(_transaction.TransactionId, default))
                .ReturnsAsync(apiResponse.Content).Verifiable();

            //Act
            var result = await _handler.Handle(_query, default).ConfigureAwait(false);

            //Assert
            var reader = new StreamReader(result.Stream);
            string text = await reader.ReadToEndAsync().ConfigureAwait(false);

            using (new AssertionScope())
            {
                result.ContentType.Should().Be(expectedMediaType);
                result.FileName.Should().Be(expectedFileName);

                text.Should().Be(SetupStreamText);
            }
        }
    }
}
