using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Refit;
using VPay.Gateway.Common.MemberClaims.Models;

namespace VPay.Gateway.Common.MemberClaims.Tests;

/// <summary>
/// Tests for <see cref="ReceivePaymentDecision.CommandHandler"/>.
/// </summary>
public class ReceivePaymentDecisionCommandHandlerTests
{
    private readonly Mock<IMemberClaimsApi> _memberClaimsApiMock = new();
    private readonly Mock<ILogger<ReceivePaymentDecision.CommandHandler>> _loggerMock = new();

    private readonly ReceivePaymentDecision.CommandHandler _handler;

    public ReceivePaymentDecisionCommandHandlerTests()
    {
        _handler = new ReceivePaymentDecision.CommandHandler(_memberClaimsApiMock.Object, _loggerMock.Object);
    }

    public class Constructor : ReceivePaymentDecisionCommandHandlerTests
    {
        [Fact]
        public void NullMemberClaimsApi_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ReceivePaymentDecision.CommandHandler(null!, _loggerMock.Object));
        }

        [Fact]
        public void NullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ReceivePaymentDecision.CommandHandler(_memberClaimsApiMock.Object, null!));
        }
    }

    public class Handle : ReceivePaymentDecisionCommandHandlerTests
    {
        [Fact]
        public async Task ValidRequest_ReturnsResponseSuccessfully()
        {
            // Arrange
            var paymentInfo = new VendorPaymentDecision { PreTransId = "123" };
            var command = new ReceivePaymentDecision.Command(paymentInfo);

            var expectedResponse = ReceivePaymentDecisionOkResponse();

            _memberClaimsApiMock
                .Setup(api => api.ReceivePaymentInfo(paymentInfo, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(expectedResponse.StatusCode, result.StatusCode);
            Assert.Equivalent(expectedResponse.Content, result.Content);
            _memberClaimsApiMock.Verify(api => api.ReceivePaymentInfo(paymentInfo, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task InvalidRequest_ReturnsResponseSuccessfully()
        {
            // Arrange
            var paymentInfo = new VendorPaymentDecision { PreTransId = "123" };
            var command = new ReceivePaymentDecision.Command(paymentInfo);

            var expectedResponse = ReceivePaymentDecisionValidationErrorResponse();

            _memberClaimsApiMock
                .Setup(api => api.ReceivePaymentInfo(paymentInfo, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(expectedResponse.StatusCode, result.StatusCode);
            Assert.NotNull(expectedResponse.Content);
            Assert.Equal(expectedResponse.Content?.Status, result.Content?.Status);
            Assert.Equal(expectedResponse.Content?.Title, result.Content?.Title);
            Assert.Equal(expectedResponse.Content?.Errors, result.Content?.Errors);
            _memberClaimsApiMock.Verify(api => api.ReceivePaymentInfo(paymentInfo, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ConflictRequest_ReturnsResponseSuccessfully()
        {
            // Arrange
            var paymentInfo = new VendorPaymentDecision { PreTransId = "123" };
            var command = new ReceivePaymentDecision.Command(paymentInfo);

            var expectedResponse = ReceivePaymentDecisionConflictResponse();

            _memberClaimsApiMock
                .Setup(api => api.ReceivePaymentInfo(paymentInfo, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(expectedResponse.StatusCode, result.StatusCode);
            Assert.Equivalent(expectedResponse.Content, result.Content);
            _memberClaimsApiMock.Verify(api => api.ReceivePaymentInfo(paymentInfo, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task NotFoundRequest_ReturnsResponseSuccessfully()
        {
            // Arrange
            var paymentInfo = new VendorPaymentDecision { PreTransId = "123" };
            var command = new ReceivePaymentDecision.Command(paymentInfo);

            var expectedResponse = ReceivePaymentDecisionNotFoundResponse();

            _memberClaimsApiMock
                .Setup(api => api.ReceivePaymentInfo(paymentInfo, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(expectedResponse.StatusCode, result.StatusCode);
            Assert.Equivalent(expectedResponse.Content, result.Content);
            _memberClaimsApiMock.Verify(api => api.ReceivePaymentInfo(paymentInfo, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ApiThrowsException_LogsErrorAndReThrows()
        {
            // Arrange
            var paymentInfo = new VendorPaymentDecision { PreTransId = "123" };
            var command = new ReceivePaymentDecision.Command(paymentInfo);

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.example.com/receive-payment");
            var httpResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                RequestMessage = httpRequest
            };
            var apiException = ApiException.Create("Some error", httpRequest, HttpMethod.Post, httpResponse, new RefitSettings(), null).Result;

            _memberClaimsApiMock
                .Setup(api => api.ReceivePaymentInfo(paymentInfo, It.IsAny<CancellationToken>()))
                .ThrowsAsync(apiException);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ApiException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.Equal(apiException, exception);
        }

        [Fact]
        public async Task NullRequest_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _handler.Handle(null!, CancellationToken.None));
        }
    }

    private static ApiResponse<dynamic> ReceivePaymentDecisionOkResponse()
    {
        var content = new
        {
            Message = "Payment request received successfully.",
            TraceId = Guid.NewGuid().ToString()
        };
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            RequestMessage = new HttpRequestMessage()
        };
        var apiResponse = new ApiResponse<dynamic>(httpResponse, content, new RefitSettings());
        return apiResponse;
    }

    private static ApiResponse<dynamic> ReceivePaymentDecisionConflictResponse()
    {
        var content = new
        {
            Type = "https://httpstatuses.com/409",
            Title = "Resource Conflict",
            Status = StatusCodes.Status409Conflict,
            Detail = "Conflict error",
            TraceId = Guid.NewGuid().ToString()
        };
        var httpResponse = new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            RequestMessage = new HttpRequestMessage()
        };
        var apiResponse = new ApiResponse<dynamic>(httpResponse, content, new RefitSettings());
        return apiResponse;
    }

    private static ApiResponse<dynamic> ReceivePaymentDecisionValidationErrorResponse()
    {
        var content = new ValidationProblemDetails
        {
            Errors = new Dictionary<string, string[]>
            {
                { "PreTransId", new[] { "PreTransId is required." } }
            },
            Status = StatusCodes.Status400BadRequest,
            Type = "https://httpstatuses.com/400",
            Title = "One or more validation errors occurred."
        };
        var httpResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            RequestMessage = new HttpRequestMessage()
        };
        var apiResponse = new ApiResponse<dynamic>(httpResponse, content, new RefitSettings());
        return apiResponse;
    }

    private static ApiResponse<dynamic> ReceivePaymentDecisionNotFoundResponse()
    {
        var content = (new
        {
            Type = "https://httpstatuses.com/404",
            Title = "Resource Not Found",
            Status = StatusCodes.Status404NotFound,
            Detail = "Not found error",
            TraceId = Guid.NewGuid().ToString()
        });
        var httpResponse = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            RequestMessage = new HttpRequestMessage()
        };
        var apiResponse = new ApiResponse<dynamic>(httpResponse, content, new RefitSettings());
        return apiResponse;
    }
}
