using System.Net;
using AutoFixture;
using FluentAssertions;
using FluentAssertions.Execution;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Refit;
using VPay.Extensions.Exceptions;
using VPay.Gateway.Api.Controllers;
using VPay.Gateway.Api.Models;
using VPay.Gateway.Application;
using VPay.Gateway.Common.Kubera;
using VPay.Gateway.Common.Kubera.Models;
using VPay.Gateway.Common.Loaf;
using VPay.Gateway.Common.RestPayments;
using VPay.Gateway.Common.Tests.Helpers;
using Xunit;

namespace VPay.Gateway.Api.Tests.Controllers;

public class PaymentsControllerTests
{
    private readonly Mock<IUserInfo> _userInfo;
    private readonly Mock<IMediator> _mediator;
    private readonly ILogger<PaymentsController> _logger;
    private readonly IFixture _fixture;

    public PaymentsControllerTests()
    {
        _userInfo = new Mock<IUserInfo>();
        _mediator = new Mock<IMediator>();
        _logger = new NullLogger<PaymentsController>();
        _fixture = new Fixture();
    }

    #region Constructor Tester
    public class Constructor : PaymentsControllerTests
    {
#pragma warning disable CA1806
        [Fact]
        public void UserInfoMissing_Throws_ArgumentNullException() =>
            new Action(() => new PaymentsController(null, _mediator.Object, _logger))
                .Should().ThrowExactly<ArgumentNullException>()
                .And.ParamName.Should().Be("userInfo");

        [Fact]
        public void MediatorMissing_Throws_ArgumentNullException() =>
            new Action(() => new PaymentsController(_userInfo.Object, null, _logger))
                .Should().ThrowExactly<ArgumentNullException>()
                .And.ParamName.Should().Be("mediator");

        [Fact]
        public void LoggerMissing_Throws_ArgumentNullException() =>
            new Action(() => new PaymentsController(_userInfo.Object, _mediator.Object, null))
                .Should().ThrowExactly<ArgumentNullException>()
                .And.ParamName.Should().Be("logger");
#pragma warning restore CA1806
    }
    #endregion

    #region Void Payment
    public class VoidPaymentTests : PaymentsControllerTests
    {
        [Fact]
        public async Task BadRequestApiResponse_ReturnsBadRequestResponse()
        {
            var controller = new PaymentsController(_userInfo.Object, _mediator.Object, _logger);
            var response = _fixture.Build<VoidPayment.Response>().Create();

            _userInfo.Setup(x => x.CanAccess(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
            CreateTransactionInfoCall(_mediator);

            _mediator.Setup(x => x.Send(It.IsAny<VoidPayment.Query>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response.FakeApiResponse(HttpStatusCode.BadRequest));

            var result = await controller.VoidPayment(-1L, CancellationToken.None).ConfigureAwait(false);
            result.Should().NotBeNull().And.BeOfType<BadRequestObjectResult>();
            var typedResult = (BadRequestObjectResult)result;
            typedResult.Value.Should().NotBeNull().And.BeOfType<ValidationProblemDetails>();
            var probDetails = (ValidationProblemDetails)typedResult.Value;
            probDetails?.Errors.Should().NotBeNull();
            probDetails?.Errors.Keys.Count.Should().Be(3);
            probDetails?.Errors["Reason"].Length.Should().Be(2);
            probDetails?.Errors["Response"].Length.Should().Be(2);
            probDetails?.Errors["Success"].Length.Should().Be(2);

            probDetails?.Errors["Reason"].Should().BeEquivalentTo(response.ReasonCode, response.ReasonDesc);
            probDetails?.Errors["Response"].Should().BeEquivalentTo(response.ResponseCode, response.ResponseDesc);
            probDetails?.Errors["Success"].Should().BeEquivalentTo(response.SuccessCode, response.SuccessDesc);
        }

        [Fact]
        public async Task UnauthorizedApiResponse_ReturnsUnauthorizedResponse()
        {
            var controller = new PaymentsController(_userInfo.Object, _mediator.Object, _logger);
            var response = _fixture.Build<VoidPayment.Response>().Create();

            CreateTransactionInfoCall(_mediator);

            _mediator.Setup(x => x.Send(It.IsAny<VoidPayment.Query>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response.FakeApiResponse(HttpStatusCode.Unauthorized));

            var result = await controller.VoidPayment(-1L, CancellationToken.None).ConfigureAwait(false);
            result.Should().NotBeNull().And.BeOfType<UnauthorizedResult>();
        }

        [Fact]
        public async Task OkApiResponse_ReturnsOkResponse()
        {
            var controller = new PaymentsController(_userInfo.Object, _mediator.Object, _logger);
            var response = _fixture.Build<VoidPayment.Response>().Create();

            _userInfo.Setup(x => x.CanAccess(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
            CreateTransactionInfoCall(_mediator);

            _mediator.Setup(x => x.Send(It.IsAny<VoidPayment.Query>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response.FakeApiResponse(HttpStatusCode.OK));

            var result = await controller.VoidPayment(-1L, CancellationToken.None).ConfigureAwait(false);
            result.Should().NotBeNull().And.BeOfType<OkObjectResult>();
            var typedResult = (OkObjectResult)result;
            typedResult.Value.Should().NotBeNull().And.BeEquivalentTo(response);
        }

        [Fact]
        public async Task OddApiResponse_Returns500Response()
        {
            var controller = new PaymentsController(_userInfo.Object, _mediator.Object, _logger);

            var response = _fixture.Build<VoidPayment.Response>().Create();
            var errorMessage = _fixture.Create<string>();
            var apiExceptionResponse = await ApiException.Create(errorMessage, new HttpRequestMessage(),
                new HttpMethod("abc"), new HttpResponseMessage(HttpStatusCode.InternalServerError), new RefitSettings());

            _userInfo.Setup(x => x.CanAccess(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
            CreateTransactionInfoCall(_mediator);

            _mediator.Setup(x => x.Send(It.IsAny<VoidPayment.Query>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response.FakeApiResponse(HttpStatusCode.GatewayTimeout, apiExceptionResponse));

            var result = await controller.VoidPayment(-1L, CancellationToken.None).ConfigureAwait(false);
            result.Should().NotBeNull().And.BeOfType<StatusCodeResult>();
            var typedResult = (StatusCodeResult)result;
            typedResult.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task ClaimMissingPermission_ReturnsUnauthorized()
        {
            var controller = new PaymentsController(_userInfo.Object, _mediator.Object, _logger);
            var response = _fixture.Build<VoidPayment.Response>().Create();

            _userInfo.Setup(x => x.CanAccess(It.IsAny<string>(), It.IsAny<string>())).Returns(false);
            CreateTransactionInfoCall(_mediator);

            _mediator.Setup(x => x.Send(It.IsAny<VoidPayment.Query>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response.FakeApiResponse(HttpStatusCode.OK));

            var result = await controller.VoidPayment(123L, CancellationToken.None).ConfigureAwait(false);
            result.Should().NotBeNull().And.BeOfType<UnauthorizedResult>();
        }

        [Fact]
        public async Task TransactionInfoReportsNotFound_ReturnsNotFound()
        {
            var controller = new PaymentsController(_userInfo.Object, _mediator.Object, _logger);
            var response = _fixture.Build<VoidPayment.Response>().Create();

            CreateTransactionInfoCall(_mediator, HttpStatusCode.NotFound);

            _mediator.Setup(x => x.Send(It.IsAny<VoidPayment.Query>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response.FakeApiResponse(HttpStatusCode.OK));

            var result = await controller.VoidPayment(123L, CancellationToken.None).ConfigureAwait(false);
            result.Should().NotBeNull().And.BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task TransactionInfoReportsUnknownStatus_Returns500()
        {
            var controller = new PaymentsController(_userInfo.Object, _mediator.Object, _logger);
            var response = _fixture.Build<VoidPayment.Response>().Create();

            CreateTransactionInfoCall(_mediator, HttpStatusCode.GatewayTimeout);

            _mediator.Setup(x => x.Send(It.IsAny<VoidPayment.Query>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response.FakeApiResponse(HttpStatusCode.OK));

            var result = await controller.VoidPayment(123L, CancellationToken.None).ConfigureAwait(false);
            result.Should().NotBeNull().And.BeOfType<StatusCodeResult>();
            ((StatusCodeResult)result).StatusCode.Should().Be(500);
        }
    }
    #endregion

    #region Get Card Details
    public class GetCardDetailsTests : PaymentsControllerTests
    {
        [Fact]
        public async Task GetCardDetails_WorksAsExpected()
        {
            var controller = new PaymentsController(_userInfo.Object, _mediator.Object, _logger);
            var getCardDetailsResponse = _fixture.Build<GetCardDetails.Response>().Create();
            var paymentNumberToTransactionIdResponse = _fixture.Build<PayerSubPayerAndPaymentNumberToTransactionId.Response>().Create();

            _mediator.Setup(x => x.Send(It.Is<PayerSubPayerAndPaymentNumberToTransactionId.Query>(q => q.SubPayerId == "SubPayer" && q.PaymentId == "123"), It.IsAny<CancellationToken>()))
                .ReturnsAsync(paymentNumberToTransactionIdResponse)
                .Verifiable();

            _mediator.Setup(x =>
                    x.Send(
                        It.Is<GetCardDetails.Query>(q =>
                            q.ClientCode == paymentNumberToTransactionIdResponse.ClientCode &&
                            q.BillingEntityCode == paymentNumberToTransactionIdResponse.BillingEntityCode &&
                            q.TransactionId == paymentNumberToTransactionIdResponse.TransactionId),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(getCardDetailsResponse.FakeApiResponse(HttpStatusCode.OK))
                .Verifiable();

            _userInfo.Setup(x => x.CanAccess(It.Is<string>(q => q == paymentNumberToTransactionIdResponse.ClientCode),
                    It.Is<string>(q => q == paymentNumberToTransactionIdResponse.BillingEntityCode)))
                .Returns(true)
                .Verifiable();

            var result = await controller.GetCardDetails("PayerId", "SubPayer", "123", CancellationToken.None).ConfigureAwait(false);
            result.Should().NotBeNull().And.BeOfType<OkObjectResult>();
            var typedResult = (OkObjectResult)result;
            typedResult.Value.Should().NotBeNull().And.BeEquivalentTo(getCardDetailsResponse);
        }

        [Fact]
        public async Task GetCardDetails_PI_NotFound_ReturnsNotFound()
        {
            var controller = new PaymentsController(_userInfo.Object, _mediator.Object, _logger);

            _mediator.Setup(x => x.Send(It.Is<PayerSubPayerAndPaymentNumberToTransactionId.Query>(q => q.SubPayerId == "SubPayer" && q.PaymentId == "123"), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ItemNotFoundException())
                .Verifiable();

            var result = await controller.GetCardDetails("PayerId", "SubPayer", "123", CancellationToken.None).ConfigureAwait(false);
            result.Should().NotBeNull().And.BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task GetCardDetails_PI_Error_Returns500Error()
        {
            var controller = new PaymentsController(_userInfo.Object, _mediator.Object, _logger);

            _mediator.Setup(x => x.Send(It.Is<PayerSubPayerAndPaymentNumberToTransactionId.Query>(q => q.SubPayerId == "SubPayer" && q.PaymentId == "123"), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException())
                .Verifiable();

            var result = await controller.GetCardDetails("PayerId", "SubPayer", "123", CancellationToken.None).ConfigureAwait(false);
            result.Should().NotBeNull().And.BeOfType<StatusCodeResult>();
            ((StatusCodeResult)result).StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task GetCardDetails_CanAccess_False_ReturnsUnauthorizedError()
        {
            var controller = new PaymentsController(_userInfo.Object, _mediator.Object, _logger);
            var paymentNumberToTransactionIdResponse = _fixture.Build<PayerSubPayerAndPaymentNumberToTransactionId.Response>().Create();

            _mediator.Setup(x => x.Send(It.Is<PayerSubPayerAndPaymentNumberToTransactionId.Query>(q => q.SubPayerId == "SubPayer" && q.PaymentId == "123"), It.IsAny<CancellationToken>()))
                .ReturnsAsync(paymentNumberToTransactionIdResponse)
                .Verifiable();

            _userInfo.Setup(x => x.CanAccess(It.Is<string>(q => q == paymentNumberToTransactionIdResponse.ClientCode),
                    It.Is<string>(q => q == paymentNumberToTransactionIdResponse.BillingEntityCode)))
                .Returns(false)
                .Verifiable();

            var result = await controller.GetCardDetails("PayerId", "SubPayer", "123", CancellationToken.None).ConfigureAwait(false);
            result.Should().NotBeNull().And.BeOfType<UnauthorizedResult>();
        }

        [Fact]
        public async Task GetCardDetails_CD_NotFound_ReturnsNotFound()
        {
            var controller = new PaymentsController(_userInfo.Object, _mediator.Object, _logger);
            var getCardDetailsResponse = _fixture.Build<GetCardDetails.Response>().Create();
            var paymentNumberToTransactionIdResponse = _fixture.Build<PayerSubPayerAndPaymentNumberToTransactionId.Response>().Create();

            _mediator.Setup(x => x.Send(It.Is<PayerSubPayerAndPaymentNumberToTransactionId.Query>(q => q.SubPayerId == "SubPayer" && q.PaymentId == "123"), It.IsAny<CancellationToken>()))
                .ReturnsAsync(paymentNumberToTransactionIdResponse)
                .Verifiable();

            _mediator.Setup(x =>
                    x.Send(
                        It.Is<GetCardDetails.Query>(q =>
                            q.ClientCode == paymentNumberToTransactionIdResponse.ClientCode &&
                            q.BillingEntityCode == paymentNumberToTransactionIdResponse.BillingEntityCode &&
                            q.TransactionId == paymentNumberToTransactionIdResponse.TransactionId),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(getCardDetailsResponse.FakeApiResponse(HttpStatusCode.NotFound))
                .Verifiable();

            _userInfo.Setup(x => x.CanAccess(It.Is<string>(q => q == paymentNumberToTransactionIdResponse.ClientCode),
                    It.Is<string>(q => q == paymentNumberToTransactionIdResponse.BillingEntityCode)))
                .Returns(true)
                .Verifiable();

            var result = await controller.GetCardDetails("PayerId", "SubPayer", "123", CancellationToken.None).ConfigureAwait(false);
            result.Should().NotBeNull().And.BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task GetCardDetails_CD_OtherError_Returns500Error()
        {
            var controller = new PaymentsController(_userInfo.Object, _mediator.Object, _logger);
            var getCardDetailsResponse = _fixture.Build<GetCardDetails.Response>().Create();
            var paymentNumberToTransactionIdResponse = _fixture.Build<PayerSubPayerAndPaymentNumberToTransactionId.Response>().Create();

            _mediator.Setup(x => x.Send(It.Is<PayerSubPayerAndPaymentNumberToTransactionId.Query>(q => q.SubPayerId == "SubPayer" && q.PaymentId == "123"), It.IsAny<CancellationToken>()))
                .ReturnsAsync(paymentNumberToTransactionIdResponse)
                .Verifiable();

            _mediator.Setup(x =>
                    x.Send(
                        It.Is<GetCardDetails.Query>(q =>
                            q.ClientCode == paymentNumberToTransactionIdResponse.ClientCode &&
                            q.BillingEntityCode == paymentNumberToTransactionIdResponse.BillingEntityCode &&
                            q.TransactionId == paymentNumberToTransactionIdResponse.TransactionId),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(getCardDetailsResponse.FakeApiResponse(HttpStatusCode.GatewayTimeout))
                .Verifiable();

            _userInfo.Setup(x => x.CanAccess(It.Is<string>(q => q == paymentNumberToTransactionIdResponse.ClientCode),
                    It.Is<string>(q => q == paymentNumberToTransactionIdResponse.BillingEntityCode)))
                .Returns(true)
                .Verifiable();

            var result = await controller.GetCardDetails("PayerId", "SubPayer", "123", CancellationToken.None).ConfigureAwait(false);
            result.Should().NotBeNull().And.BeOfType<StatusCodeResult>();
            ((StatusCodeResult)result).StatusCode.Should().Be(500);
        }
    }
    #endregion

    #region Get Transaction
    public class GetTransaction : PaymentsControllerTests
    {
        private const string ClientCode = "ABC";
        private const string BillingEntityCode = "ABC0001001";
        private const string PaymentId = "123abc";

        [Fact]
        public async Task WhenTransactionsNotFound_ReturnsNotFound()
        {
            _mediator
                .Setup(x => x.Send(It.Is<GetTransactionsByPaymentIdQuery>(q => q.ClientCode == ClientCode && q.PaymentId == PaymentId), CancellationToken.None))
                .ReturnsAsync(Array.Empty<Transaction>());

            ActionResult actionResult = await PerformApiCall().ConfigureAwait(false);
            actionResult.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task WhenTransactionsFoundForDifferentBillingEntity_ReturnsNotFound()
        {
            _mediator
                .Setup(x => x.Send(It.Is<GetTransactionsByPaymentIdQuery>(q => q.ClientCode == ClientCode && q.PaymentId == PaymentId), CancellationToken.None))
                .ReturnsAsync(new Transaction[]
                {
                    new() {
                        ClientCode = ClientCode,
                        BillingEntityCode = "GGG0001001"
                    }
                });

            ActionResult actionResult = await PerformApiCall().ConfigureAwait(false);
            actionResult.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task WhenTransactionsFound_AndUserDoesNotHavePermissions_ReturnsNotFound()
        {
            _mediator
                .Setup(x => x.Send(It.Is<GetTransactionsByPaymentIdQuery>(q => q.ClientCode == ClientCode && q.PaymentId == PaymentId), CancellationToken.None))
                .ReturnsAsync(new Transaction[]
                {
                    new() {
                        ClientCode = ClientCode,
                        BillingEntityCode = BillingEntityCode,
                        PaymentTypeCode = "CLVCP"
                    }
                });

            _userInfo
                .Setup(x => x.CanAccess(ClientCode, BillingEntityCode, "CLVCP"))
                .Returns(false);

            ActionResult actionResult = await PerformApiCall().ConfigureAwait(false);
            actionResult.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task WhenTransactionsFound_AndUserHasPermissions_ReturnsTransaction()
        {
            _mediator
                .Setup(x => x.Send(It.Is<GetTransactionsByPaymentIdQuery>(q => q.ClientCode == ClientCode && q.PaymentId == PaymentId), CancellationToken.None))
                .ReturnsAsync(new Transaction[]
                {
                    new() {
                        TransactionId = 837,
                        ClientCode = ClientCode,
                        BillingEntityCode = BillingEntityCode,
                        PaymentTypeCode = "CLVCP"
                    }
                });

            _userInfo
                .Setup(x => x.CanAccess(ClientCode, BillingEntityCode, "CLVCP"))
                .Returns(true);

            ActionResult actionResult = await PerformApiCall().ConfigureAwait(false);
            actionResult.Should().BeOfType<OkObjectResult>()
                .Which.Value.Should().BeEquivalentTo(
                new TransactionInformationDto
                {
                    ClientCode = ClientCode,
                    BillingEntityCode = BillingEntityCode,
                    TransactionId = 837,
                    ClientPaymentId = PaymentId
                });
        }

        private async Task<ActionResult> PerformApiCall()
        {
            var controller = new PaymentsController(_userInfo.Object, _mediator.Object, _logger);
            var result = await controller.GetTransaction(ClientCode, BillingEntityCode, PaymentId).ConfigureAwait(false);
            return result.Result;
        }
    }
    #endregion

    #region Reject Payment
    public class RejectPayment : PaymentsControllerTests
    {
        [Fact]
        public async Task WhenRequestIsValid_ReturnsOk()
        {
            var controller = new PaymentsController(_userInfo.Object, _mediator.Object, _logger);

            var request = new PaymentRejectionRequestDto
            {
                NetworkPartner = "NETWORK",
                OptumTransactionId = 123456789,
                RejectionReasonCode = "UTLA",
                RejectionReason = "SOME REASON"
            };

            var expectedCommand = new RejectPaymentCommand
            (
                request.NetworkPartner,
                request.OptumTransactionId.Value,
                request.RejectionReasonCode,
                request.RejectionReason,
                false
            );
            var expectedResult = new RejectPaymentResult(
                request.NetworkPartner,
                request.OptumTransactionId.Value,
                request.RejectionReasonCode
            );

            _mediator
                .Setup(x => x.Send(It.Is<RejectPaymentCommand>(q => q.Equals(expectedCommand)), default))
                .ReturnsAsync(expectedResult)
                .Verifiable();

            var result = await controller
                .RejectNetworkPayment(request, default);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();
                result.Result.Should().BeOfType<OkObjectResult>().Should().NotBeNull();

                var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
                ok.Should().NotBeNull();
                ok.StatusCode.Should().Be(200);

                var response = ok.Value as PaymentRejectionResultDto;
                response.Should().NotBeNull();
                response.NetworkPartner.Should().Be(request.NetworkPartner);
                response.OptumTransactionId.Should().Be(request.OptumTransactionId.Value);
                response.ResponseMessage.Should().Be(request.RejectionReasonCode);
            }

            _mediator.VerifyAll();
        }

        [Fact]
        public async Task WhenRequestIsInvalid_ReturnsBadRequest()
        {
            var controller = new PaymentsController(_userInfo.Object, _mediator.Object, _logger);

            var request = new PaymentRejectionRequestDto
            {
                NetworkPartner = "NETWORK",
                OptumTransactionId = 0,  // <-- Invalid TransactionId
                RejectionReasonCode = "UTLA",
                RejectionReason = "SOME REASON"
            };

            const string ErrorMessage = "Validation failed";

            var result = await controller
                .RejectNetworkPayment(request, default);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();
                result.Result.Should().BeOfType<BadRequestObjectResult>().Should().NotBeNull();

                var bad = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
                bad.Should().NotBeNull();
                bad.Value.Should().NotBeNull();
                bad.Value.Should().ToString().StartsWith(ErrorMessage, StringComparison.OrdinalIgnoreCase);
                bad.StatusCode.Should().Be(400);
            }

            _mediator.VerifyAll();
        }

        [Fact]
        public async Task WhenTransactionOrNetworkNotFound_ReturnsBadRequest()
        {
            var controller = new PaymentsController(_userInfo.Object, _mediator.Object, _logger);

            var request = new PaymentRejectionRequestDto
            {
                NetworkPartner = "NETWORK",
                OptumTransactionId = 123456789,
                RejectionReasonCode = "UTLA",
                RejectionReason = "SOME REASON"
            };

            var expectedCommand = new RejectPaymentCommand
            (
                request.NetworkPartner,
                request.OptumTransactionId.Value,
                request.RejectionReasonCode,
                request.RejectionReason,
                false
            );

            const string ErrorMessage = "Some error";
            var expectedException = new ItemNotFoundException(ErrorMessage);

            _mediator
                .Setup(x => x.Send(It.Is<RejectPaymentCommand>(q => q.Equals(expectedCommand)), default))
                .ThrowsAsync(expectedException)
                .Verifiable();

            var result = await controller
                .RejectNetworkPayment(request, default);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();
                result.Result.Should().BeOfType<BadRequestObjectResult>().Should().NotBeNull();

                var bad = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
                bad.Should().NotBeNull();
                bad.Value.Should().NotBeNull();
                bad.Value.Should().Be(ErrorMessage);
                bad.StatusCode.Should().Be(400);
            }

            _mediator.VerifyAll();
        }

        [Fact]
        public async Task WhenUserNotAuthenticatedOrHasNoAccess_ReturnsForbid()
        {
            var controller = new PaymentsController(_userInfo.Object, _mediator.Object, _logger);

            var request = new PaymentRejectionRequestDto
            {
                NetworkPartner = "NETWORK",
                OptumTransactionId = 123456789,
                RejectionReasonCode = "UTLA",
                RejectionReason = "SOME REASON"
            };

            var expectedCommand = new RejectPaymentCommand
            (
                request.NetworkPartner,
                request.OptumTransactionId.Value,
                request.RejectionReasonCode,
                request.RejectionReason,
                false
            );

            const string ErrorMessage = "Some error";
            var expectedException = new NotAuthorizedException(ErrorMessage);

            _mediator
                .Setup(x => x.Send(It.Is<RejectPaymentCommand>(q => q.Equals(expectedCommand)), default))
                .ThrowsAsync(expectedException)
                .Verifiable();

            var result = await controller
                .RejectNetworkPayment(request, default);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Result.Should().NotBeNull();
                result.Result.Should().BeOfType<ForbidResult>().Should().NotBeNull();

                var forbid = result.Result.Should().BeOfType<ForbidResult>().Subject;
                forbid.Should().NotBeNull();
            }

            _mediator.VerifyAll();
        }
    }
    #endregion

    #region Helpers
    private static void CreateTransactionInfoCall(Mock<IMediator> mediatorMock, HttpStatusCode httpStatusCode = HttpStatusCode.OK)
    {
        var transactionInfoAPIResponse = new TransactionInfo.Response(123, "CC", "BEC").FakeApiResponse(httpStatusCode);

        mediatorMock.Setup(x => x.Send(It.IsAny<TransactionInfo.Query>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactionInfoAPIResponse);
    }
    #endregion
}
