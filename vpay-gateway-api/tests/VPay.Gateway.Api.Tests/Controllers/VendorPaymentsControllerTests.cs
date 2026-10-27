using System.Net;
using AutoMapper; // Add this namespace for IMapper
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Refit;
using Vpay.Gateway.Common.Vpay.Models;
using VPay.Extensions.Exceptions;
using VPay.Gateway.Api.Controllers;
using VPay.Gateway.Api.Models;
using VPay.Gateway.Application;
using VPay.Gateway.Common.Documents;
using VPay.Gateway.Common.MemberClaims;
using VPay.Gateway.Common.MemberClaims.Models;
using VPay.Gateway.Common.PlatformConfig;
using VPay.Gateway.Common.RestPayments;
using Xunit;

namespace VPay.Gateway.Api.Tests.Controllers;

public class VendorPaymentsControllerTests
{
    private readonly Mock<IUserInfo> _userInfo;
    private readonly Mock<IMediator> _mediator;
    private readonly Mock<IMapper> _mapper; // Add IMapper mock
    private readonly ILogger<VendorPaymentsController> _logger;
    private readonly VendorPaymentsController _controller;

    private const long TransactionId = 123456789L;
    private const string PreTransactionId = "Z123456";

    private const string UserName = "TestUser";
    private const string ClientCode = "ABC";
    private const string BillingEntityCode = "ABC0001001";

    private const string PayerCode = "PAYER123";
    private const string SubPayerCode = "SUBPAYER456";
    private const string ClientMemberId = "CLIENT123";
    private const string ZetaMemberId = "ZETA123";
    private const string BankRoutingNumber = "123456789";
    private const string BankAccountNumber = "987654321";

    public VendorPaymentsControllerTests()
    {
        _userInfo = new Mock<IUserInfo>();
        _mediator = new Mock<IMediator>();
        _mapper = new Mock<IMapper>(); // Initialize IMapper mock
        _logger = new NullLogger<VendorPaymentsController>();
        _controller = new VendorPaymentsController(
          _userInfo.Object,
          _mediator.Object,
          _mapper.Object,
          _logger
      );
    }

    public class GetPaymentDetail : VendorPaymentsControllerTests
    {
        private static ApiResponse<TransactionInfo.Response> TransactionInfoOkResponse()
        {
            var content = new TransactionInfo.Response(TransactionId, ClientCode, BillingEntityCode);
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                RequestMessage = new HttpRequestMessage()
            };
            var apiResponse = new ApiResponse<TransactionInfo.Response>(httpResponse, content, new RefitSettings());
            return apiResponse;
        }

        private static ApiResponse<TransactionInfo.Response> TransactionInfoErrorResponse(HttpStatusCode statusCode, string errorMessage)
        {
            var httpRequest = new HttpRequestMessage();
            var content = new TransactionInfo.Response(TransactionId, ClientCode, BillingEntityCode);
            var httpResponse = new HttpResponseMessage(statusCode)
            {
                RequestMessage = httpRequest
            };
            var apiError = ApiException.Create(errorMessage, httpRequest, HttpMethod.Post, httpResponse, new RefitSettings(), null).Result;
            var apiResponse = new ApiResponse<TransactionInfo.Response>(httpResponse, content, new RefitSettings(), apiError);
            return apiResponse;
        }

        [Fact]
        public async Task ReturnsValidationProblem_WhenNoIdsProvided()
        {
            // Act
            var result = await _controller.GetPaymentDetail(null, null);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Result);
            var validationResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(validationResult);
            Assert.NotNull(validationResult.Value);
            var detailsResult = Assert.IsType<ValidationProblemDetails>(validationResult.Value);
            Assert.Equal(StatusCodes.Status400BadRequest, detailsResult.Status);
            validationResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        }

        [Fact]
        public async Task ReturnsBadRequest_WhenTransactionIdNotFoundByPreTransactionId()
        {
            // Arrange
            _mediator
                .Setup(m => m.Send(It.IsAny<GetTransactionbyPreTransIdQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(0L);

            // Act
            var result = await _controller.GetPaymentDetail(PreTransactionId, null);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result.Result);
            objectResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        }

        [Fact]
        public async Task ReturnsNotFound_WhenTransactionInfoNotFound()
        {
            // Arrange
            var transactionInfo = TransactionInfoErrorResponse(HttpStatusCode.NotFound, "Transaction not found");
            _mediator
                .Setup(m => m.Send(It.IsAny<TransactionInfo.Query>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(transactionInfo);

            // Act
            var result = await _controller.GetPaymentDetail(null, TransactionId);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result.Result);
            objectResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        }

        [Fact]
        public async Task ReturnsBadRequest_WhenTransactionInfoUnsuccessful()
        {
            // Arrange
            var transactionInfo = TransactionInfoErrorResponse(HttpStatusCode.BadRequest, "Invalid Transaction ID");
            _mediator
                .Setup(m => m.Send(It.IsAny<TransactionInfo.Query>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(transactionInfo);

            // Act
            var result = await _controller.GetPaymentDetail(null, TransactionId);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result.Result);
            objectResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        }

        [Fact]
        public async Task ReturnsUnauthorized_WhenUserCannotAccess()
        {
            // Arrange
            var transactionInfo = TransactionInfoOkResponse();
            _mediator
                .Setup(m => m.Send(It.IsAny<TransactionInfo.Query>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(transactionInfo);
            _userInfo
                .Setup(u => u.CanAccess(ClientCode, BillingEntityCode))
                .Returns(false);

            // Act
            var result = await _controller.GetPaymentDetail(null, TransactionId);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result.Result);
            objectResult.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        }

        [Fact]
        public async Task ReturnsNotFound_WhenPreTransactionIdNotFound()
        {
            // Arrange
            var transactionInfo = TransactionInfoOkResponse();
            _mediator
                .Setup(m => m.Send(It.IsAny<TransactionInfo.Query>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(transactionInfo);
            _userInfo
                .Setup(u => u.CanAccess(ClientCode, BillingEntityCode))
                .Returns(true);
            _mediator
                .Setup(m => m.Send(It.IsAny<GetPreTransIdbyTransactionQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string)null);

            // Act
            var result = await _controller.GetPaymentDetail(null, TransactionId);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Result);
            var objectResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.NotNull(objectResult);
            var detailsResult = Assert.IsType<Microsoft.AspNetCore.Mvc.ProblemDetails>(objectResult.Value);
            Assert.NotNull(detailsResult);
            objectResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        }

        [Fact]
        public async Task ReturnsNotFound_WhenVendorPaymentDetailIsNull()
        {
            // Arrange
            var transactionInfo = TransactionInfoOkResponse();
            _mediator
                .Setup(m => m.Send(It.IsAny<TransactionInfo.Query>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(transactionInfo);
            _userInfo
                .Setup(u => u.CanAccess(ClientCode, BillingEntityCode))
                .Returns(true);
            _mediator
                .Setup(m => m.Send(It.IsAny<GetPreTransIdbyTransactionQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(PreTransactionId);
            _mediator
                .Setup(m => m.Send(It.IsAny<GetVendorPaymentDetailQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((TransactionSearchResponseVendor)null);

            // Act
            var result = await _controller.GetPaymentDetail(null, TransactionId);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result.Result);
            objectResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        }

        [Fact]
        public async Task ReturnsOk_WhenAllValid()
        {
            // Arrange
            var transactionInfo = TransactionInfoOkResponse();
            var paymentDetail = new TransactionSearchResponseVendor
            {
                vpayTransactionID = TransactionId,
                PreTransID = PreTransactionId
            };
            _mediator
                .Setup(m => m.Send(It.IsAny<TransactionInfo.Query>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(transactionInfo);
            _userInfo
                .Setup(u => u.CanAccess(ClientCode, BillingEntityCode))
                .Returns(true);
            _mediator
                .Setup(m => m.Send(It.IsAny<GetPreTransIdbyTransactionQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(PreTransactionId);
            _mediator
                .Setup(m => m.Send(It.IsAny<GetVendorPaymentDetailQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(paymentDetail);

            // Act
            var result = await _controller.GetPaymentDetail(null, TransactionId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
            okResult.Value.Should().Be(paymentDetail);
        }

        [Fact]
        public async Task ReturnsOkResult_WhenValidRequest()
        {
            // Arrange
            var transactionInfoResponse = new TransactionInfo.Response(TransactionId, ClientCode, BillingEntityCode);
            var transactionSearchResponse = new TransactionSearchResponseVendor
            {
                vpayTransactionID = TransactionId,
                AvailableBalance = 100
            };

            _userInfo.Setup(u => u.DisplayName).Returns(UserName);
            _userInfo.Setup(u => u.CanAccess(ClientCode, BillingEntityCode)).Returns(true);

            _mediator
                .Setup(m => m.Send(It.IsAny<TransactionInfo.Query>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ApiResponse<TransactionInfo.Response>(
                    new HttpResponseMessage(HttpStatusCode.OK),
                    transactionInfoResponse,
                    null,
                    null
                ));

            _mediator
                .Setup(m => m.Send(It.IsAny<GetVendorPaymentDetailQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(transactionSearchResponse);

            // Act
            var result = await _controller.GetPaymentDetail(PreTransactionId, TransactionId, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var actualResponse = Assert.IsType<TransactionSearchResponseVendor>(okResult.Value);
            Assert.Equal(transactionSearchResponse.vpayTransactionID, actualResponse.vpayTransactionID);
        }

        [Fact]
        public async Task ReturnsNotFound_WhenTransactionInfoReturnsNull()
        {
            // Arrange
            var request = new TransactionSearchRequestVendorDto
            {
                PayerCode = PayerCode,
                SubPayerCode = SubPayerCode,
                PreTransactionId = PreTransactionId,
                TransactionId = TransactionId
            };
            var transactionInfoResponse = new TransactionInfo.Response(TransactionId, ClientCode, BillingEntityCode);
            var transactionSearchResponse = new TransactionSearchResponseVendor
            {
                vpayTransactionID = TransactionId,
                AvailableBalance = 100
            };
            _userInfo.Setup(u => u.DisplayName).Returns(UserName);
            _userInfo.Setup(u => u.CanAccess(ClientCode, BillingEntityCode)).Returns(true);
            _mediator
               .Setup(m => m.Send(It.IsAny<TransactionInfo.Query>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new ApiResponse<TransactionInfo.Response>(
                   new HttpResponseMessage(HttpStatusCode.OK),
                   transactionInfoResponse,
                   null,
                   null
               ));

            // Act
            var result = await _controller.GetPaymentDetail(
                request.PreTransactionId,
                request.TransactionId,
                CancellationToken.None
            );

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Result);
            var objectResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal((int)HttpStatusCode.NotFound, objectResult.StatusCode);
        }

        [Fact]
        public async Task ReturnsValidationProblem_WhenPreTransactionIdIsInvalid()
        {
            // Arrange: preTransactionId is not numeric after stripping
            var invalidPreTransactionId = "Zabc";
            // Act
            var result = await _controller.GetPaymentDetail(invalidPreTransactionId, null);
            // Assert
            var validationResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var details = Assert.IsType<ValidationProblemDetails>(validationResult.Value);
            Assert.Contains("Invalid PreTransactionId", details.Errors["preTransactionId"][0]);
        }

        [Fact]
        public async Task ReturnsValidationProblem_WhenTransactionIdIsZeroOrNegative()
        {
            // Act
            var result = await _controller.GetPaymentDetail(null, 0);
            // Assert
            var validationResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var details = Assert.IsType<ValidationProblemDetails>(validationResult.Value);
            Assert.Contains("TransactionId must be greater than 0", details.Errors["transactionId"][0]);
        }

        [Fact]
        public async Task ReturnsProblem_WhenMediatorThrowsKnownExceptions()
        {
            // Arrange
            _mediator.Setup(m => m.Send(It.IsAny<TransactionInfo.Query>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ItemNotFoundException("not found"));
            // Act
            var result = await _controller.GetPaymentDetail(null, TransactionId);
            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(404, objectResult.StatusCode);

            // BadRequestException
            _mediator.Setup(m => m.Send(It.IsAny<TransactionInfo.Query>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new BadRequestException("bad request"));
            result = await _controller.GetPaymentDetail(null, TransactionId);
            objectResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(400, objectResult.StatusCode);

            // ConflictException
            _mediator.Setup(m => m.Send(It.IsAny<TransactionInfo.Query>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ConflictException("conflict"));
            result = await _controller.GetPaymentDetail(null, TransactionId);
            objectResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(409, objectResult.StatusCode);

            // NotAuthorizedException
            _mediator.Setup(m => m.Send(It.IsAny<TransactionInfo.Query>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new NotAuthorizedException("forbidden"));
            result = await _controller.GetPaymentDetail(null, TransactionId);
            objectResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(403, objectResult.StatusCode);

            // General Exception
            _mediator.Setup(m => m.Send(It.IsAny<TransactionInfo.Query>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("fail"));
            result = await _controller.GetPaymentDetail(null, TransactionId);
            objectResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(400, objectResult.StatusCode);
        }
    }

    public class StopPaymentTests : VendorPaymentsControllerTests
    {
        [Fact]
        public async Task ReturnsStatusCode500_WhenTransactionInfoContentIsNull()
        {
            var request = new VendorStopPaymentRequestDto { TransactionId = TransactionId, RequesterName = "req" };
            var apiResponse = Mock.Of<IApiResponse<TransactionInfo.Response>>(r =>
                r.StatusCode == HttpStatusCode.OK && r.Content == null && r.Error == null);

            _mediator.Setup(m => m.Send(It.IsAny<TransactionInfo.Query>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(apiResponse);

            var result = await _controller.StopPayment(request);
            var objectResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
        }

        [Fact]
        public async Task ReturnsStatusCode500_WhenUnloadVendorPaymentResponseIsUnknown()
        {
            var request = new VendorStopPaymentRequestDto { TransactionId = TransactionId, RequesterName = "req" };
            var transactionInfo = new TransactionInfo.Response(TransactionId, ClientCode, BillingEntityCode);
            var apiResponse = Mock.Of<IApiResponse<TransactionInfo.Response>>(r =>
                r.StatusCode == HttpStatusCode.OK && r.Content == transactionInfo);

            _mediator.Setup(m => m.Send(It.IsAny<TransactionInfo.Query>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(apiResponse);
            _userInfo.Setup(u => u.CanAccess(ClientCode, BillingEntityCode)).Returns(true);

            var errorApiResponse = Mock.Of<IApiResponse<UnloadVendorPaymentResponse>>(r =>
                r.StatusCode == HttpStatusCode.InternalServerError);

            _mediator.Setup(m => m.Send(It.IsAny<StopVendorPayment.Query>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(errorApiResponse);

            var result = await _controller.StopPayment(request);
            var objectResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
        }
    }

    public class VoidPayment : VendorPaymentsControllerTests
    {
        [Fact]
        public async Task ReturnsStatusCode500_WhenUnloadVendorPaymentResponseIsUnknown()
        {
            var request = new VendorVoidPaymentRequestDto { TransactionId = TransactionId, RequesterName = "req" };
            var transactionInfo = new TransactionInfo.Response(TransactionId, ClientCode, BillingEntityCode);
            var apiResponse = Mock.Of<IApiResponse<TransactionInfo.Response>>(r =>
                r.StatusCode == HttpStatusCode.OK && r.Content == transactionInfo);

            _mediator.Setup(m => m.Send(It.IsAny<TransactionInfo.Query>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(apiResponse);
            _userInfo.Setup(u => u.CanAccess(ClientCode, BillingEntityCode)).Returns(true);

            var errorApiResponse = Mock.Of<IApiResponse<UnloadVendorPaymentResponse>>(r =>
                r.StatusCode == HttpStatusCode.InternalServerError);

            _mediator.Setup(m => m.Send(It.IsAny<UnloadVendorPayment.Query>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(errorApiResponse);

            var result = await _controller.VoidPayment(request);
            var objectResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
        }
    }

    public class VoidPaymentWithReasonCode : VendorPaymentsControllerTests
    {
        [Fact]
        public async Task ReturnsStatusCode500_WhenUnloadVendorPaymentResponseIsUnknown()
        {
            var request = new VendorVoidPaymentWithReasonCodeRequestDto { TransactionId = TransactionId, RequesterName = "req", ReasonCode = "RC" };
            var transactionInfo = new TransactionInfo.Response(TransactionId, ClientCode, BillingEntityCode);
            var apiResponse = Mock.Of<IApiResponse<TransactionInfo.Response>>(r =>
                r.StatusCode == HttpStatusCode.OK && r.Content == transactionInfo);

            _mediator.Setup(m => m.Send(It.IsAny<TransactionInfo.Query>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(apiResponse);
            _userInfo.Setup(u => u.CanAccess(ClientCode, BillingEntityCode)).Returns(true);

            var errorApiResponse = Mock.Of<IApiResponse<UnloadVendorPaymentResponse>>(r =>
                r.StatusCode == HttpStatusCode.InternalServerError);

            _mediator.Setup(m => m.Send(It.IsAny<UnloadVendorPayment.Query>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(errorApiResponse);

            var result = await _controller.VoidPaymentWithReasonCode(request);
            var objectResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
        }
    }

    public class GetDocumentList : VendorPaymentsControllerTests
    {
        [Fact]
        public async Task ReturnsStatusCode500_OnException()
        {
            _mediator.Setup(m => m.Send(It.IsAny<GetFilteredDocumentsByTransactionIdQuery>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("fail"));

            var result = await _controller.GetDocumentList(TransactionId);
            var objectResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, objectResult.StatusCode);
        }

        [Fact]
        public async Task ReturnsNotFound_OnNotAuthorizedException()
        {
            _mediator.Setup(m => m.Send(It.IsAny<GetFilteredDocumentsByTransactionIdQuery>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new NotAuthorizedException("forbidden"));

            var result = await _controller.GetDocumentList(TransactionId);
            var objectResult = Assert.IsType<NotFoundResult>(result.Result);
            Assert.Equal(404, objectResult.StatusCode);
        }
    }

    public class GetDocument : VendorPaymentsControllerTests
    {
        [Fact]
        public async Task ReturnsStatusCode500_OnException()
        {
            _mediator.Setup(m => m.Send(It.IsAny<DownloadDocumentByIdQuery>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("fail"));

            var result = await _controller.GetDocument(TransactionId, 1);

            Assert.NotNull(result);
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.NotNull(objectResult);
            Assert.Equal(500, objectResult.StatusCode);
        }

        [Fact]
        public async Task ReturnsNotFound_OnNotAuthorizedException()
        {
            _mediator.Setup(m => m.Send(It.IsAny<DownloadDocumentByIdQuery>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new NotAuthorizedException("forbidden"));

            var result = await _controller.GetDocument(TransactionId, 1);
            var objectResult = Assert.IsType<NotFoundResult>(result);
            Assert.Equal(404, objectResult.StatusCode);
        }

        [Fact]
        public async Task ReturnsNotFound_OnDocumentNotInArchiveException()
        {
            _mediator.Setup(m => m.Send(It.IsAny<DownloadDocumentByIdQuery>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DocumentNotInArchiveException("not found"));

            var result = await _controller.GetDocument(TransactionId, 1);
            var objectResult = Assert.IsType<NotFoundResult>(result);
            Assert.Equal(404, objectResult.StatusCode);
        }
    }

    public class AddBankAccountTests : VendorPaymentsControllerTests
    {
        [Fact]
        public async Task ReturnsOk_WhenRequestIsValid()
        {
            // Arrange
            var request = GetValidRequest();

            var billingEntityDto = new BillingEntityDto
            {
                ClientCode = ClientCode,
                BillingEntityCode = BillingEntityCode
            };

            var originClientDto = new OriginClientDto
            {
                Origin = new OriginInfoDto
                {
                    OriginCode = "ORIGIN123"
                }
            };

            var addBankAccountResponse = new AddBankAccount.AddBankAccountResponse(
                id: "1",
                vPayMemberId: "12345678901234567000",
                successCode: "SUCCESS",
                successDesc: "Bank account added successfully"
            );

            var apiResponse = Mock.Of<IApiResponse<AddBankAccount.AddBankAccountResponse>>(r =>
                r.StatusCode == HttpStatusCode.OK && r.Content == addBankAccountResponse);

            _userInfo
                .Setup(u => u.DisplayName).Returns(UserName);
            _mediator
                .Setup(m => m.Send(It.IsAny<BillingEntityQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(billingEntityDto);
            _mediator
                .Setup(m => m.Send(It.IsAny<OriginClientQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(originClientDto);
            _mediator
                .Setup(m => m.Send(It.IsAny<AddBankAccount.Command>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _controller.AddBankAccount(request, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<AddBankAccountSuccessResponse>(okResult.Value);
            Assert.Equal("12345678901234567000", response.OptumMemberId);
            Assert.Equal("success", response.Status);
        }

        [Fact]
        public async Task ReturnsValidationError_WhenOriginClientQueryReturnsNull()
        {
            // Arrange
            var request = GetValidRequest();
            var billingEntityDto = new BillingEntityDto
            {
                ClientCode = ClientCode,
                BillingEntityCode = BillingEntityCode
            };
            var errors = new Dictionary<string, string[]>
            {
                { "PayerCode", new[] { "Invalid Payer Code" } }
            };
            _mediator
                .Setup(m => m.Send(It.IsAny<BillingEntityQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(billingEntityDto);
            _mediator
                .Setup(m => m.Send(It.IsAny<OriginClientQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((OriginClientDto)null);
            // Act
            var result = await _controller.AddBankAccount(request);
            // Assert
            Assert.NotNull(result);
            var problemResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(problemResult.Value);
            Assert.Equal(problemResult.StatusCode, (int)HttpStatusCode.BadRequest);
            var problemDetails = Assert.IsType<Microsoft.AspNetCore.Mvc.ValidationProblemDetails>(problemResult.Value);
            Assert.Equal("One or more validation errors occurred", problemDetails.Title);
            Assert.Equal(errors, problemDetails.Errors);
        }

        [Fact]
        public async Task ReturnsValidationError_WhenBillingEntityQueryReturnsNull()
        {
            // Arrange
            var request = GetValidRequest();
            var originClientDto = new OriginClientDto
            {
                Origin = new OriginInfoDto { OriginCode = "ORIGIN" }
            };
            var errors = new Dictionary<string, string[]>
            {
                { "SubPayerCode", new[] { "Invalid Sub-Payer Code" } }
            };
            _mediator
                .Setup(m => m.Send(It.IsAny<BillingEntityQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((BillingEntityDto)null);
            _mediator
                .Setup(m => m.Send(It.IsAny<OriginClientQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(originClientDto);
            // Act
            var result = await _controller.AddBankAccount(request);
            // Assert
            Assert.NotNull(result);
            var problemResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(problemResult.Value);
            Assert.Equal(problemResult.StatusCode, (int)HttpStatusCode.BadRequest);
            var problemDetails = Assert.IsType<ValidationProblemDetails>(problemResult.Value);
            Assert.Equal("One or more validation errors occurred", problemDetails.Title);
            Assert.Equal(errors, problemDetails.Errors);
        }

        [Fact]
        public async Task ReturnsBadRequest_WhenInvalidInput()
        {
            // Arrange
            var request = GetValidRequest();

            var billingEntityDto = new BillingEntityDto
            {
                ClientCode = ClientCode,
                BillingEntityCode = BillingEntityCode
            };

            var originClientDto = new OriginClientDto
            {
                Origin = new OriginInfoDto
                {
                    OriginCode = "ORIGIN123"
                }
            };

            var apiResponse = Mock.Of<IApiResponse<AddBankAccount.AddBankAccountResponse>>(r =>
                r.StatusCode == HttpStatusCode.BadRequest);

            _mediator
                .Setup(m => m.Send(It.IsAny<BillingEntityQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(billingEntityDto);
            _mediator
               .Setup(m => m.Send(It.IsAny<OriginClientQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(originClientDto);
            _mediator
                .Setup(m => m.Send(It.IsAny<AddBankAccount.Command>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _controller.AddBankAccount(request, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            var problemResult = Assert.IsType<ObjectResult>(result);
            Assert.NotNull(problemResult.Value);
            Assert.Equal(problemResult.StatusCode, (int)HttpStatusCode.BadRequest);
            var problemDetails = Assert.IsType<Microsoft.AspNetCore.Mvc.ProblemDetails>(problemResult.Value);
            Assert.Equal("Invalid Input", problemDetails.Title);
            Assert.Equal("Missing required fields or invalid data", problemDetails.Detail);
        }

        [Fact]
        public async Task ReturnsUnauthorized_WhenUnauthorized()
        {
            // Arrange
            var request = GetValidRequest();

            var billingEntityDto = new BillingEntityDto
            {
                ClientCode = ClientCode,
                BillingEntityCode = BillingEntityCode
            };

            var originClientDto = new OriginClientDto
            {
                Origin = new OriginInfoDto { OriginCode = "ORIGIN" }
            };

            _mediator
                .Setup(m => m.Send(It.IsAny<BillingEntityQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(billingEntityDto);
            _mediator
                .Setup(m => m.Send(It.IsAny<OriginClientQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(originClientDto);
            _userInfo
                .Setup(x => x.CanAccess(request.PayerCode, request.SubPayerCode)).Returns(true);
            _mediator
                .Setup(m => m.Send(It.IsAny<AddBankAccount.Command>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Mock.Of<IApiResponse<AddBankAccount.AddBankAccountResponse>>(r =>
                    r.StatusCode == HttpStatusCode.Unauthorized));

            // Act
            var result = await _controller.AddBankAccount(request);

            // Assert
            Assert.NotNull(result);
            var problemResult = Assert.IsType<ObjectResult>(result);
            Assert.NotNull(problemResult.Value);
            Assert.Equal(problemResult.StatusCode, (int)HttpStatusCode.Unauthorized);
            var problemDetails = Assert.IsType<Microsoft.AspNetCore.Mvc.ProblemDetails>(problemResult.Value);
            Assert.Equal("Unauthorized Request", problemDetails.Title);
            Assert.Equal("User not allowed to perform this action", problemDetails.Detail);
        }

        [Fact]
        public async Task ReturnsBadRequest_WhenUnexpectedErrorOccurs()
        {
            // Arrange
            var request = GetValidRequest();

            var billingEntityDto = new BillingEntityDto
            {
                ClientCode = ClientCode,
                BillingEntityCode = BillingEntityCode
            };

            var originClientDto = new OriginClientDto
            {
                Origin = new OriginInfoDto
                {
                    OriginCode = "ORIGIN123"
                }
            };

            _mediator
                .Setup(m => m.Send(It.IsAny<BillingEntityQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(billingEntityDto);
            _mediator
               .Setup(m => m.Send(It.IsAny<OriginClientQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(originClientDto);
            _mediator
                .Setup(m => m.Send(It.IsAny<AddBankAccount.Command>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Unexpected error"));

            // Act
            var result = await _controller.AddBankAccount(request, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            var problemResult = Assert.IsType<ObjectResult>(result);
            Assert.NotNull(problemResult.Value);
            Assert.Equal(problemResult.StatusCode, (int)HttpStatusCode.BadRequest);
            var problemDetails = Assert.IsType<Microsoft.AspNetCore.Mvc.ProblemDetails>(problemResult.Value);
            Assert.Equal("Unexpected Error", problemDetails.Title);
            Assert.Equal("An unexpected error occurred", problemDetails.Detail);
        }

        [Fact]
        public async Task ReturnsUnexpectedError_WhenBillingEntityQueryThrowsException()
        {
            // Arrange
            var request = GetValidRequest();

            var originClientDto = new OriginClientDto
            {
                Origin = new OriginInfoDto { OriginCode = "ORIGIN" }
            };

            _mediator
                .Setup(m => m.Send(It.IsAny<BillingEntityQuery>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("fail"));
            _mediator
                .Setup(m => m.Send(It.IsAny<OriginClientQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(originClientDto);

            // Act
            var result = await _controller.AddBankAccount(request);

            // Assert
            Assert.NotNull(result);
            var problemResult = Assert.IsType<ObjectResult>(result);
            Assert.NotNull(problemResult.Value);
            Assert.Equal(problemResult.StatusCode, (int)HttpStatusCode.BadRequest);
            var problemDetails = Assert.IsType<Microsoft.AspNetCore.Mvc.ProblemDetails>(problemResult.Value);
            Assert.Equal("Unexpected Error", problemDetails.Title);
            Assert.Equal("An unexpected error occurred", problemDetails.Detail);
        }

        [Fact]
        public async Task ReturnsUnexpectedError_WhenOriginClientQueryThrowsException()
        {
            // Arrange
            var request = GetValidRequest();

            var billingEntityDto = new BillingEntityDto
            {
                ClientCode = ClientCode,
                BillingEntityCode = BillingEntityCode
            };

            _mediator
                .Setup(m => m.Send(It.IsAny<BillingEntityQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(billingEntityDto);
            _mediator
                .Setup(m => m.Send(It.IsAny<OriginClientQuery>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("fail"));

            // Act
            var result = await _controller.AddBankAccount(request);

            // Assert
            Assert.NotNull(result);
            var problemResult = Assert.IsType<ObjectResult>(result);
            Assert.NotNull(problemResult.Value);
            Assert.Equal(problemResult.StatusCode, (int)HttpStatusCode.BadRequest);
            var problemDetails = Assert.IsType<Microsoft.AspNetCore.Mvc.ProblemDetails>(problemResult.Value);
            Assert.Equal("Unexpected Error", problemDetails.Title);
            Assert.Equal("An unexpected error occurred", problemDetails.Detail);
        }

        private static VendorAddBankAccountDto GetValidRequest()
        {
            return new VendorAddBankAccountDto
            {
                PayerCode = PayerCode,
                SubPayerCode = SubPayerCode,
                ClientMemberId = ClientMemberId,
                ZetaMemberId = ZetaMemberId,
                BankRoutingNumber = BankRoutingNumber,
                BankAccountNumber = BankAccountNumber
            };
        }
    }

    public class ExpireBankAccountTests : VendorPaymentsControllerTests
    {
        [Fact]
        public async Task ReturnsOk_WhenSuccess()
        {
            // Arrange
            var request = GetValidRequest();

            var billingEntityDto = new BillingEntityDto
            {
                ClientCode = ClientCode,
                BillingEntityCode = BillingEntityCode
            };

            var originClientDto = new OriginClientDto
            {
                Origin = new OriginInfoDto { OriginCode = "ORIGIN" }
            };
            var expireResponse = new ExpireBankAccount.ExpireBankAccountResponse(
                status: "OK",
                optumMemberId: "OPTUMID",
                successDesc: null
            );

            _mediator
                .Setup(m => m.Send(It.IsAny<BillingEntityQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(billingEntityDto);
            _mediator
                .Setup(m => m.Send(It.IsAny<OriginClientQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(originClientDto);
            _userInfo
                .Setup(x => x.CanAccess(request.PayerCode, request.SubPayerCode)).Returns(true);
            _mediator
                .Setup(m => m.Send(It.IsAny<ExpireBankAccount.ExpireBankAccountCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Mock.Of<IApiResponse<ExpireBankAccount.ExpireBankAccountResponse>>(r =>
                    r.StatusCode == HttpStatusCode.OK && r.Content == expireResponse));

            // Act
            var result = await _controller.ExpireBankAccount(request);

            // Assert
            var okResult = result as OkObjectResult;
            okResult.Should().NotBeNull();
            var response = okResult.Value as ExpireBankAccountSuccessResponse;
            response.Should().NotBeNull();
            response.OptumMemberId.Should().Be("OPTUMID");
        }

        [Fact]
        public async Task ReturnsValidationError_WhenOriginClientQueryReturnsNull()
        {
            // Arrange
            var request = GetValidRequest();
            var billingEntityDto = new BillingEntityDto
            {
                ClientCode = ClientCode,
                BillingEntityCode = BillingEntityCode
            };
            var errors = new Dictionary<string, string[]>
            {
                { "PayerCode", new[] { "Invalid Payer Code" } }
            };
            _mediator
                .Setup(m => m.Send(It.IsAny<BillingEntityQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(billingEntityDto);
            _mediator
                .Setup(m => m.Send(It.IsAny<OriginClientQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((OriginClientDto)null);
            // Act
            var result = await _controller.ExpireBankAccount(request);
            // Assert
            Assert.NotNull(result);
            var problemResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(problemResult.Value);
            Assert.Equal(problemResult.StatusCode, (int)HttpStatusCode.BadRequest);
            var problemDetails = Assert.IsType<Microsoft.AspNetCore.Mvc.ValidationProblemDetails>(problemResult.Value);
            Assert.Equal("One or more validation errors occurred", problemDetails.Title);
            Assert.Equal(errors, problemDetails.Errors);
        }

        [Fact]
        public async Task ReturnsValidationError_WhenBillingEntityQueryReturnsNull()
        {
            // Arrange
            var request = GetValidRequest();
            var originClientDto = new OriginClientDto
            {
                Origin = new OriginInfoDto { OriginCode = "ORIGIN" }
            };
            var errors = new Dictionary<string, string[]>
            {
                { "SubPayerCode", new[] { "Invalid Sub-Payer Code" } }
            };
            _mediator
                .Setup(m => m.Send(It.IsAny<BillingEntityQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((BillingEntityDto)null);
            _mediator
                .Setup(m => m.Send(It.IsAny<OriginClientQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(originClientDto);
            // Act
            var result = await _controller.ExpireBankAccount(request);
            // Assert
            Assert.NotNull(result);
            var problemResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(problemResult.Value);
            Assert.Equal(problemResult.StatusCode, (int)HttpStatusCode.BadRequest);
            var problemDetails = Assert.IsType<Microsoft.AspNetCore.Mvc.ValidationProblemDetails>(problemResult.Value);
            Assert.Equal("One or more validation errors occurred", problemDetails.Title);
            Assert.Equal(errors, problemDetails.Errors);
        }

        [Fact]
        public async Task ReturnsBadRequest_WhenBadRequest()
        {
            // Arrange
            var request = GetValidRequest();

            var billingEntityDto = new BillingEntityDto
            {
                ClientCode = ClientCode,
                BillingEntityCode = BillingEntityCode
            };

            var originClientDto = new OriginClientDto
            {
                Origin = new OriginInfoDto { OriginCode = "ORIGIN" }
            };

            _mediator
                .Setup(m => m.Send(It.IsAny<BillingEntityQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(billingEntityDto);
            _mediator
                .Setup(m => m.Send(It.IsAny<OriginClientQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(originClientDto);
            _userInfo
                .Setup(x => x.CanAccess(request.PayerCode, request.SubPayerCode)).Returns(true);
            _mediator
                .Setup(m => m.Send(It.IsAny<ExpireBankAccount.ExpireBankAccountCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Mock.Of<IApiResponse<ExpireBankAccount.ExpireBankAccountResponse>>(r => r.StatusCode == HttpStatusCode.BadRequest));

            // Act
            var result = await _controller.ExpireBankAccount(request);

            // Assert
            Assert.NotNull(result);
            var problemResult = Assert.IsType<ObjectResult>(result);
            Assert.NotNull(problemResult.Value);
            Assert.Equal(problemResult.StatusCode, (int)HttpStatusCode.BadRequest);
            var problemDetails = Assert.IsType<Microsoft.AspNetCore.Mvc.ProblemDetails>(problemResult.Value);
            Assert.Equal("Invalid Input", problemDetails.Title);
            Assert.Equal("Missing required fields or invalid data", problemDetails.Detail);
        }

        [Fact]
        public async Task ReturnsUnauthorized_WhenUnauthorized()
        {
            // Arrange
            var request = GetValidRequest();

            var billingEntityDto = new BillingEntityDto
            {
                ClientCode = ClientCode,
                BillingEntityCode = BillingEntityCode
            };

            var originClientDto = new OriginClientDto
            {
                Origin = new OriginInfoDto { OriginCode = "ORIGIN" }
            };

            _mediator
                .Setup(m => m.Send(It.IsAny<BillingEntityQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(billingEntityDto);
            _mediator
                .Setup(m => m.Send(It.IsAny<OriginClientQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(originClientDto);
            _userInfo
                .Setup(x => x.CanAccess(request.PayerCode, request.SubPayerCode)).Returns(true);
            _mediator
                .Setup(m => m.Send(It.IsAny<ExpireBankAccount.ExpireBankAccountCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Mock.Of<IApiResponse<ExpireBankAccount.ExpireBankAccountResponse>>(r =>
                    r.StatusCode == HttpStatusCode.Unauthorized));

            // Act
            var result = await _controller.ExpireBankAccount(request);

            // Assert
            Assert.NotNull(result);
            var problemResult = Assert.IsType<ObjectResult>(result);
            Assert.NotNull(problemResult.Value);
            Assert.Equal(problemResult.StatusCode, (int)HttpStatusCode.Unauthorized);
            var problemDetails = Assert.IsType<Microsoft.AspNetCore.Mvc.ProblemDetails>(problemResult.Value);
            Assert.Equal("Unauthorized Request", problemDetails.Title);
            Assert.Equal("User not allowed to perform this action", problemDetails.Detail);
        }

        [Fact]
        public async Task ReturnsUnauthorized_WhenUserCannotAccess()
        {
            // Arrange
            var request = GetValidRequest();

            var billingEntityDto = new BillingEntityDto
            {
                ClientCode = ClientCode,
                BillingEntityCode = BillingEntityCode
            };

            var originClientDto = new OriginClientDto
            {
                Origin = new OriginInfoDto { OriginCode = "ORIGIN" }
            };

            _mediator
                .Setup(m => m.Send(It.IsAny<BillingEntityQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(billingEntityDto);
            _mediator
                .Setup(m => m.Send(It.IsAny<OriginClientQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(originClientDto);
            _userInfo
                .Setup(x => x.CanAccess(request.PayerCode, request.SubPayerCode)).Returns(false);

            // Act
            var result = await _controller.ExpireBankAccount(request);

            // Assert
            Assert.NotNull(result);
            var problemResult = Assert.IsType<ObjectResult>(result);
            Assert.NotNull(problemResult.Value);
            Assert.Equal(problemResult.StatusCode, (int)HttpStatusCode.Unauthorized);
            var problemDetails = Assert.IsType<Microsoft.AspNetCore.Mvc.ProblemDetails>(problemResult.Value);
            Assert.Equal("Unauthorized Request", problemDetails.Title);
            Assert.Equal("User not configured for provided Payer Code and/or Sub-Payer Code", problemDetails.Detail);
        }

        [Fact]
        public async Task ReturnsUnexpectedError_WhenBillingEntityQueryThrowsException()
        {
            // Arrange
            var request = GetValidRequest();

            var originClientDto = new OriginClientDto
            {
                Origin = new OriginInfoDto { OriginCode = "ORIGIN" }
            };

            _mediator
                .Setup(m => m.Send(It.IsAny<BillingEntityQuery>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("fail"));
            _mediator
                .Setup(m => m.Send(It.IsAny<OriginClientQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(originClientDto);

            // Act
            var result = await _controller.ExpireBankAccount(request);

            // Assert
            Assert.NotNull(result);
            var problemResult = Assert.IsType<ObjectResult>(result);
            Assert.NotNull(problemResult.Value);
            Assert.Equal(problemResult.StatusCode, (int)HttpStatusCode.BadRequest);
            var problemDetails = Assert.IsType<Microsoft.AspNetCore.Mvc.ProblemDetails>(problemResult.Value);
            Assert.Equal("Unexpected Error", problemDetails.Title);
            Assert.Equal("An unexpected error occurred", problemDetails.Detail);
        }

        [Fact]
        public async Task ReturnsUnexpectedError_WhenOriginClientQueryThrowsException()
        {
            // Arrange
            var request = GetValidRequest();

            var billingEntityDto = new BillingEntityDto
            {
                ClientCode = ClientCode,
                BillingEntityCode = BillingEntityCode
            };

            _mediator
                .Setup(m => m.Send(It.IsAny<BillingEntityQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(billingEntityDto);
            _mediator
                .Setup(m => m.Send(It.IsAny<OriginClientQuery>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("fail"));

            // Act
            var result = await _controller.ExpireBankAccount(request);

            // Assert
            Assert.NotNull(result);
            var problemResult = Assert.IsType<ObjectResult>(result);
            Assert.NotNull(problemResult.Value);
            Assert.Equal(problemResult.StatusCode, (int)HttpStatusCode.BadRequest);
            var problemDetails = Assert.IsType<Microsoft.AspNetCore.Mvc.ProblemDetails>(problemResult.Value);
            Assert.Equal("Unexpected Error", problemDetails.Title);
            Assert.Equal("An unexpected error occurred", problemDetails.Detail);
        }

        private static VendorExpireBankAccountDto GetValidRequest() => new()
        {
            PayerCode = "PAYER",
            SubPayerCode = "SUBPAYER",
            ClientMemberId = "CLIENTMEMBER",
            ZetaMemberId = "ZETAMEMBER"
        };
    }

    public class ReceivePaymentDecisionTests : VendorPaymentsControllerTests
    {
        private static VendorPaymentDecisionDto ReceivePaymentDecisionRequestDto() => new()
        {
            PayerCode = "PAYER1",
            SubpayerCode = "SUBPAYER1",
            PreTransId = PreTransactionId,
            MemberIDs = [new VendorPaymentDecisionMemberDto { ZetaMemberId = "ZMEMBER1", ClientMemberId = "CLIENTMEMBER1", OptumMemberId = "12345678" }],
            DecisionedModality = "ACH",
            NoSelectionExpiredIndicator = false,
            ClaimRejectionReason = null,
            ZetaTransactionId = "ZETA123"
        };

        private static ApiResponse<VendorPaymentDecisionResponse> ReceivePaymentDecisionOkResponse()
        {
            var content = new VendorPaymentDecisionResponse
            {
                Message = "Payment request received successfully.",
                TraceId = Guid.NewGuid().ToString()
            };
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                RequestMessage = new HttpRequestMessage()
            };
            var apiResponse = new ApiResponse<VendorPaymentDecisionResponse>(httpResponse, content, new RefitSettings());
            return apiResponse;
        }

        private static ApiResponse<VendorPaymentDecisionResponse> ReceivePaymentDecisionErrorResponse(HttpStatusCode statusCode, string errorTitle, string errorMessage)
        {
            var httpRequest = new HttpRequestMessage();
            var content = new VendorPaymentDecisionResponse
            {
                Title = errorTitle,
                Status = (int)statusCode,
                Detail = errorMessage,
                TraceId = Guid.NewGuid().ToString()
            };
            var httpResponse = new HttpResponseMessage(statusCode)
            {
                RequestMessage = httpRequest
            };
            var apiError = ApiException.Create(errorMessage, httpRequest, HttpMethod.Post, httpResponse, new RefitSettings(), null).Result;
            var apiResponse = new ApiResponse<VendorPaymentDecisionResponse>(httpResponse, content, new RefitSettings(), apiError);
            return apiResponse;
        }

        private static ApiResponse<VendorPaymentDecisionResponse> ReceivePaymentDecisionValidationErrorResponse(HttpStatusCode statusCode, string errorTitle, string errorMessage, IDictionary<string, string[]> errors)
        {
            var httpRequest = new HttpRequestMessage();
            var content = new VendorPaymentDecisionResponse
            {
                Title = errorTitle,
                Status = (int)statusCode,
                Detail = errorMessage,
                Errors = errors,
                Instance = "about:blank"
            };
            var httpResponse = new HttpResponseMessage(statusCode)
            {
                RequestMessage = httpRequest
            };
            var apiError = ApiException.Create(errorMessage, httpRequest, HttpMethod.Post, httpResponse, new RefitSettings(), null).Result;
            var apiResponse = new ApiResponse<VendorPaymentDecisionResponse>(httpResponse, content, new RefitSettings(), apiError);
            return apiResponse;
        }

        private static readonly string[] _errorList = ["Error1", "Error2"];

        [Fact]
        public async Task ReturnsOk_WhenAllValid()
        {
            // Arrange
            var dto = ReceivePaymentDecisionRequestDto();
            var clientInfo = new BillingEntityDto { ClientCode = ClientCode, BillingEntityCode = BillingEntityCode };
            var memberClaimsApiResponse = ReceivePaymentDecisionOkResponse();

            _userInfo.Setup(u => u.DisplayName).Returns(UserName);
            _userInfo.Setup(u => u.CanAccess(dto.PayerCode, dto.SubpayerCode)).Returns(true);
            _mediator.Setup(m => m.Send(It.IsAny<BillingEntityQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(clientInfo);
            _mediator.Setup(m => m.Send(It.IsAny<ReceivePaymentDecision.Command>(), It.IsAny<CancellationToken>())).ReturnsAsync(memberClaimsApiResponse);

            // Act
            var result = await _controller.ReceivePaymentDecision(dto, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            var vpResult = Assert.IsType<VendorPaymentResponseDto>(okResult.Value);
            Assert.NotNull(vpResult);
        }

        [Fact]
        public async Task ReturnsValidationProblem_WhenClientInfoIsNull()
        {
            // Arrange
            var dto = ReceivePaymentDecisionRequestDto();

            _userInfo.Setup(u => u.DisplayName).Returns(UserName);
            _mediator.Setup(m => m.Send(It.IsAny<BillingEntityQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync((BillingEntityDto)null);

            // Act
            var result = await _controller.ReceivePaymentDecision(dto, CancellationToken.None);

            // Assert
            var validationResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, validationResult.StatusCode);
            var validationProblemDetails = Assert.IsType<ValidationProblemDetails>(validationResult.Value);
            Assert.NotNull(validationProblemDetails);
            Assert.True(validationProblemDetails.Errors.ContainsKey("PayerCode"));
            Assert.True(validationProblemDetails.Errors.ContainsKey("SubpayerCode"));
        }

        [Fact]
        public async Task ReturnsUnauthorized_WhenUserCannotAccess()
        {
            // Arrange
            var dto = ReceivePaymentDecisionRequestDto();
            var clientInfo = new BillingEntityDto { ClientCode = ClientCode, BillingEntityCode = BillingEntityCode };

            _userInfo.Setup(u => u.DisplayName).Returns(UserName);
            _userInfo.Setup(u => u.CanAccess(dto.PayerCode, dto.SubpayerCode)).Returns(false);
            _mediator.Setup(m => m.Send(It.IsAny<BillingEntityQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(clientInfo);

            // Act
            var result = await _controller.ReceivePaymentDecision(dto, CancellationToken.None);

            // Assert
            var unauthorizedResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(401, unauthorizedResult.StatusCode);
            var problemDetails = Assert.IsType<Microsoft.AspNetCore.Mvc.ProblemDetails>(unauthorizedResult.Value);
            Assert.Contains("Unauthorized", problemDetails.Title, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ReturnsProblem_WhenResponseIsNull()
        {
            // Arrange
            var dto = ReceivePaymentDecisionRequestDto();
            var clientInfo = new BillingEntityDto { ClientCode = ClientCode, BillingEntityCode = BillingEntityCode };

            _userInfo.Setup(u => u.DisplayName).Returns(UserName);
            _userInfo.Setup(u => u.CanAccess(dto.PayerCode, dto.SubpayerCode)).Returns(true);
            _mediator.Setup(m => m.Send(It.IsAny<BillingEntityQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(clientInfo);
            _mediator.Setup(m => m.Send(It.IsAny<ReceivePaymentDecision.Command>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ApiResponse<VendorPaymentDecisionResponse>)null);

            // Act
            var result = await _controller.ReceivePaymentDecision(dto, CancellationToken.None);

            // Assert
            var problemResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(400, problemResult.StatusCode);
            var problemDetails = Assert.IsType<Microsoft.AspNetCore.Mvc.ProblemDetails>(problemResult.Value);
            Assert.NotNull(problemDetails);
            Assert.Contains("Null response", problemDetails.Detail, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ReturnsProblem_WhenResponseIsValidationProblem()
        {
            // Arrange
            var dto = ReceivePaymentDecisionRequestDto();
            var clientInfo = new BillingEntityDto { ClientCode = ClientCode, BillingEntityCode = BillingEntityCode };
            var errorTitle = "One or more validation errors occurred";
            var errorList = new Dictionary<string, string[]> { { "FieldA", _errorList } };
            var memberClaimsApiResponse = ReceivePaymentDecisionValidationErrorResponse(
                HttpStatusCode.BadRequest,
                errorTitle,
                errorMessage: null,
                errorList);

            _userInfo.Setup(u => u.DisplayName).Returns(UserName);
            _userInfo.Setup(u => u.CanAccess(dto.PayerCode, dto.SubpayerCode)).Returns(true);
            _mediator.Setup(m => m.Send(It.IsAny<BillingEntityQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(clientInfo);
            _mediator.Setup(m => m.Send(It.IsAny<ReceivePaymentDecision.Command>(), It.IsAny<CancellationToken>())).ReturnsAsync(memberClaimsApiResponse);

            // Act
            var result = await _controller.ReceivePaymentDecision(dto, CancellationToken.None);

            // Assert
            var problemResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, problemResult.StatusCode);
            var problemDetails = Assert.IsType<ValidationProblemDetails>(problemResult.Value);
            Assert.NotNull(problemDetails);
            Assert.Contains(errorTitle, problemDetails.Title, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(errorList, problemDetails.Errors);
        }

        [Fact]
        public async Task ReturnsProblem_WhenResponseIsBadRequest()
        {
            // Arrange
            var dto = ReceivePaymentDecisionRequestDto();
            var clientInfo = new BillingEntityDto { ClientCode = ClientCode, BillingEntityCode = BillingEntityCode };
            var errorTitle = "Unexpected Error";
            var errorMessage = "Member Claims API unsuccessful response";
            var memberClaimsApiResponse = ReceivePaymentDecisionErrorResponse(HttpStatusCode.BadRequest, errorTitle, errorMessage);

            _userInfo.Setup(u => u.DisplayName).Returns(UserName);
            _userInfo.Setup(u => u.CanAccess(dto.PayerCode, dto.SubpayerCode)).Returns(true);
            _mediator.Setup(m => m.Send(It.IsAny<BillingEntityQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(clientInfo);
            _mediator.Setup(m => m.Send(It.IsAny<ReceivePaymentDecision.Command>(), It.IsAny<CancellationToken>())).ReturnsAsync(memberClaimsApiResponse);

            // Act
            var result = await _controller.ReceivePaymentDecision(dto, CancellationToken.None);

            // Assert
            var problemResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(409, problemResult.StatusCode);
            var problemDetails = Assert.IsType<Microsoft.AspNetCore.Mvc.ProblemDetails>(problemResult.Value);
            Assert.NotNull(problemDetails);
            Assert.Contains(errorTitle, problemDetails.Title, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(errorMessage, problemDetails.Detail, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ReturnsProblem_WhenResponseIsNotFound()
        {
            // Arrange
            var dto = ReceivePaymentDecisionRequestDto();
            var clientInfo = new BillingEntityDto { ClientCode = ClientCode, BillingEntityCode = BillingEntityCode };
            var errorTitle = "Not Found";
            var errorMessage = "Member Claims API unsuccessful response";
            var memberClaimsApiResponse = ReceivePaymentDecisionErrorResponse(HttpStatusCode.NotFound, errorTitle, errorMessage);

            _userInfo.Setup(u => u.DisplayName).Returns(UserName);
            _userInfo.Setup(u => u.CanAccess(dto.PayerCode, dto.SubpayerCode)).Returns(true);
            _mediator.Setup(m => m.Send(It.IsAny<BillingEntityQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(clientInfo);
            _mediator.Setup(m => m.Send(It.IsAny<ReceivePaymentDecision.Command>(), It.IsAny<CancellationToken>())).ReturnsAsync(memberClaimsApiResponse);

            // Act
            var result = await _controller.ReceivePaymentDecision(dto, CancellationToken.None);

            // Assert
            var problemResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(404, problemResult.StatusCode);
            var problemDetails = Assert.IsType<Microsoft.AspNetCore.Mvc.ProblemDetails>(problemResult.Value);
            Assert.NotNull(problemDetails);
            Assert.Contains(errorTitle, problemDetails.Title, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(errorMessage, problemDetails.Detail, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ReturnsProblem_WhenResponseIsServerError()
        {
            // Arrange
            var dto = ReceivePaymentDecisionRequestDto();
            var clientInfo = new BillingEntityDto { ClientCode = ClientCode, BillingEntityCode = BillingEntityCode };
            var errorTitle = "Unexpected Error";
            var errorMessage = "Member Claims API unsuccessful response";
            var memberClaimsApiResponse = ReceivePaymentDecisionErrorResponse(HttpStatusCode.InternalServerError, errorTitle, errorMessage);

            _userInfo.Setup(u => u.DisplayName).Returns(UserName);
            _userInfo.Setup(u => u.CanAccess(dto.PayerCode, dto.SubpayerCode)).Returns(true);
            _mediator.Setup(m => m.Send(It.IsAny<BillingEntityQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(clientInfo);
            _mediator.Setup(m => m.Send(It.IsAny<ReceivePaymentDecision.Command>(), It.IsAny<CancellationToken>())).ReturnsAsync(memberClaimsApiResponse);

            // Act
            var result = await _controller.ReceivePaymentDecision(dto, CancellationToken.None);

            // Assert
            var problemResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(409, problemResult.StatusCode);
            var problemDetails = Assert.IsType<Microsoft.AspNetCore.Mvc.ProblemDetails>(problemResult.Value);
            Assert.NotNull(problemDetails);
            Assert.Contains(errorTitle, problemDetails.Title, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(errorMessage, problemDetails.Detail, StringComparison.OrdinalIgnoreCase);
        }
    }
}
