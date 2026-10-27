using System.Net;
using FluentAssertions;
using FluentAssertions.Execution;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Refit;
using VPay.Extensions.Exceptions;
using VPay.Gateway.Common.Preferencing.Models;
using VPay.Gateway.Common.RestPayments;
using VPay.Gateway.Data.Sql.Transactions;
using Xunit;

namespace VPay.Gateway.Application.Tests;

public class RejectPaymentCommandHandlerTests
{
    private readonly IMediator _mediator;
    private readonly IUserInfo _userInfo;
    private readonly ILogger<RejectPaymentCommandHandler> _logger;
    private readonly RejectPaymentCommandHandler _handler;

    private const long Id = 1;
    private const long TransactionId = 1;

    private const string ClientCode = "ABC";
    private const string BeCode = "ABC001001";
    private const string PaymentId = "ABC202401011001";

    private const string NetworkPartner = "MY_NETWORK";
    private const string EnrollmentType = "B2B";
    private const string NetworkType = "STP";
    private const string RejectionCode = "UTLA";
    private const string RejectionReason = "Unable to Locate Account";
    private const string ResponseMessage = "Do STP DTP Completion";

    public RejectPaymentCommandHandlerTests()
    {
        _mediator = Mock.Of<IMediator>(MockBehavior.Strict);
        _userInfo = Mock.Of<IUserInfo>(MockBehavior.Strict);
        _logger = new NullLogger<RejectPaymentCommandHandler>();
        _handler = new RejectPaymentCommandHandler(_mediator, _userInfo, _logger);
    }

    public class Constructor : RejectPaymentCommandHandlerTests
    {
        [Fact]
        public void WhenMediatorIsNull_ThrowsArgumentNullException()
        {
            Action act = new(() =>
            {
                var _ = new RejectPaymentCommandHandler(null!, _userInfo, _logger);
            });
            act.Should().ThrowExactly<ArgumentNullException>()
                .Which.ParamName.Should().Be("mediator");
        }

        [Fact]
        public void WhenUserInfoIsNull_ThrowsArgumentNullException()
        {
            Action act = new(() =>
            {
                var _ = new RejectPaymentCommandHandler(_mediator, null!, _logger);
            });
            act.Should().ThrowExactly<ArgumentNullException>()
                .Which.ParamName.Should().Be("userInfo");
        }

        [Fact]
        public void WhenLoggerIsNull_ThrowsArgumentNullException()
        {
            Action act = () =>
            {
                var _ = new RejectPaymentCommandHandler(_mediator, _userInfo, null!);
            };
            act.Should().ThrowExactly<ArgumentNullException>()
                .Which.ParamName.Should().Be("logger");
        }
    }

    public class Handle : RejectPaymentCommandHandlerTests
    {
        [Fact]
        public async Task WhenRequestIsNull_ThrowsArgumentNullException()
        {
            Func<Task> act = async () => await _handler.Handle(null, CancellationToken.None);
            var result = await act.Should().ThrowAsync<ArgumentNullException>();
            result.Which.ParamName.Should().Be("request");
        }

        [Fact]
        public async Task WhenNetworkPartnerNotRecognized_ThrowsItemNotFoundException()
        {
            var command = MockCommand();
            var expectedTransaction = MockTransaction();

            const string ErrorMessage = "Network Partner not recognized.";
            var expectedException = new ItemNotFoundException(ErrorMessage);

            var networkQuery = new GetNetworkQuery(NetworkPartner);
            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<GetNetworkQuery>(), default))
                .ReturnsAsync((NetworkDto)null)
                .Verifiable();

            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            using (new AssertionScope())
            {
                var result = await act.Should()
                    .ThrowAsync<ItemNotFoundException>()
                    .WithMessage(ErrorMessage);
            }

            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenTransactionIdIsNotFound_ThrowsItemNotFoundException()
        {
            var command = MockCommand();
            var expectedNetwork = MockNetwork();

            const string ErrorMessage = "Invalid Transaction Id.";
            var expectedException = new ItemNotFoundException(ErrorMessage);

            var networkQuery = new GetNetworkQuery(NetworkPartner);
            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<GetNetworkQuery>(), default))
                .ReturnsAsync(expectedNetwork)
                .Verifiable();

            var transactionIdQuery = new GetPaymentByTransactionIdQuery(TransactionId);
            Mock.Get(_mediator)
                .Setup(x => x.Send(It.Is<GetPaymentByTransactionIdQuery>(q => q.TransactionId == TransactionId), default))
                .ReturnsAsync((TransactionPayment)null)
                .Verifiable();

            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            using (new AssertionScope())
            {
                var result = await act.Should()
                    .ThrowAsync<ItemNotFoundException>()
                    .WithMessage(ErrorMessage);
            }

            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenUserNotAuthenticated_ThrowsNotAuthorizedException()
        {
            var command = MockCommand();
            var expectedNetwork = MockNetwork();
            var expectedTransaction = MockTransaction();

            const string ErrorMessage = "Client Id not authenticated.";
            var expectedException = new InvalidOperationException(ErrorMessage);

            var networkQuery = new GetNetworkQuery(NetworkPartner);
            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<GetNetworkQuery>(), default))
                .ReturnsAsync(expectedNetwork)
                .Verifiable();

            var transactionIdQuery = new GetPaymentByTransactionIdQuery(TransactionId);
            Mock.Get(_mediator)
                .Setup(x => x.Send(It.Is<GetPaymentByTransactionIdQuery>(q => q.TransactionId == TransactionId), default))
                .ReturnsAsync(expectedTransaction)
                .Verifiable();

            Mock.Get(_userInfo)
                .Setup(x => x.IsAuthenticated)
                .Returns(false)
                .Verifiable();

            Mock.Get(_userInfo)
                .Setup(x => x.ClientId)
                .Returns(ClientCode)
                .Verifiable();

            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            using (new AssertionScope())
            {
                var result = await act.Should()
                    .ThrowAsync<NotAuthorizedException>()
                    .WithMessage(ErrorMessage);
            }

            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenUserCannotAccessClient_ThrowsNotAuthorizedException()
        {
            var command = MockCommand();
            var expectedNetwork = MockNetwork();
            var expectedTransaction = MockTransaction();

            const string ErrorMessage = "Client Id not allowed for Client Code and/or Billing Entity Code.";
            var expectedException = new InvalidOperationException(ErrorMessage);

            var networkQuery = new GetNetworkQuery(NetworkPartner);
            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<GetNetworkQuery>(), default))
                .ReturnsAsync(expectedNetwork)
                .Verifiable();

            var transactionIdQuery = new GetPaymentByTransactionIdQuery(TransactionId);
            Mock.Get(_mediator)
                .Setup(x => x.Send(It.Is<GetPaymentByTransactionIdQuery>(q => q.TransactionId == TransactionId), default))
                .ReturnsAsync(expectedTransaction)
                .Verifiable();

            Mock.Get(_userInfo)
                .Setup(x => x.IsAuthenticated)
                .Returns(true)
                .Verifiable();

            Mock.Get(_userInfo)
                .Setup(x => x.CanAccess(It.Is<string>(q => q == ClientCode), It.Is<string>(q => q == BeCode)))
                .Returns(false)
                .Verifiable();

            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            using (new AssertionScope())
            {
                var result = await act.Should()
                    .ThrowAsync<NotAuthorizedException>()
                    .WithMessage(ErrorMessage);
            }

            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenTransactionNetworkNotFound_ThrowsItemNotFoundException()
        {
            var command = MockCommand();
            var expectedNetwork = MockNetwork();
            var expectedTransaction = MockTransaction();

            const string ErrorMessage = "Network Partner not associated with transaction.";
            var expectedException = new ItemNotFoundException(ErrorMessage);

            var networkQuery = new GetNetworkQuery(NetworkPartner);
            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<GetNetworkQuery>(), default))
                .ReturnsAsync(expectedNetwork)
                .Verifiable();

            var transactionIdQuery = new GetPaymentByTransactionIdQuery(TransactionId);
            Mock.Get(_mediator)
                .Setup(x => x.Send(It.Is<GetPaymentByTransactionIdQuery>(q => q.TransactionId == TransactionId), default))
                .ReturnsAsync(expectedTransaction)
                .Verifiable();

            Mock.Get(_userInfo)
                .Setup(x => x.IsAuthenticated)
                .Returns(true)
                .Verifiable();

            Mock.Get(_userInfo)
                .Setup(x => x.CanAccess(It.Is<string>(q => q == ClientCode), It.Is<string>(q => q == BeCode)))
                .Returns(true)
                .Verifiable();

            var transactionNetworkQuery = new GetNetworkByTransactionIdQuery(TransactionId);
            Mock.Get(_mediator)
                .Setup(x => x.Send(It.Is<GetNetworkByTransactionIdQuery>(q => q.TransactionId == TransactionId), default))
                .ReturnsAsync((TransactionNetwork)null)
                .Verifiable();

            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            using (new AssertionScope())
            {
                var result = await act.Should()
                    .ThrowAsync<ItemNotFoundException>()
                    .WithMessage(ErrorMessage);
            }

            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenTransactionNetworkDoesNotMatch_ThrowsItemNotFoundException()
        {
            var command = MockCommand();
            var expectedNetwork = MockNetwork();
            var expectedTransaction = MockTransaction();
            var expectedTransactionNetwork = MockOtherTransactionNetwork();

            const string ErrorMessage = "Network Partner not associated with transaction.";
            var expectedException = new ItemNotFoundException(ErrorMessage);

            var networkQuery = new GetNetworkQuery(NetworkPartner);
            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<GetNetworkQuery>(), default))
                .ReturnsAsync(expectedNetwork)
                .Verifiable();

            var transactionIdQuery = new GetPaymentByTransactionIdQuery(TransactionId);
            Mock.Get(_mediator)
                .Setup(x => x.Send(It.Is<GetPaymentByTransactionIdQuery>(q => q.TransactionId == TransactionId), default))
                .ReturnsAsync(expectedTransaction)
                .Verifiable();

            Mock.Get(_userInfo)
                .Setup(x => x.IsAuthenticated)
                .Returns(true)
                .Verifiable();

            Mock.Get(_userInfo)
                .Setup(x => x.CanAccess(It.Is<string>(q => q == ClientCode), It.Is<string>(q => q == BeCode)))
                .Returns(true)
                .Verifiable();

            var transactionNetworkQuery = new GetNetworkByTransactionIdQuery(TransactionId);
            Mock.Get(_mediator)
                .Setup(x => x.Send(It.Is<GetNetworkByTransactionIdQuery>(q => q.TransactionId == TransactionId), default))
                .ReturnsAsync(expectedTransactionNetwork)
                .Verifiable();

            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            using (new AssertionScope())
            {
                var result = await act.Should()
                    .ThrowAsync<ItemNotFoundException>()
                    .WithMessage(ErrorMessage);
            }

            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenNetworkPayorIsNotEnrolled_ThrowsItemNotFoundException()
        {
            var command = MockCommand(true);
            var expectedNetwork = MockNetwork();
            var expectedTransaction = MockTransaction();
            var expectedTransactionNetwork = MockTransactionNetwork();

            const string ErrorMessage = "Payor not recognized or not enrolled with Network.";
            var expectedException = new ItemNotFoundException(ErrorMessage);

            var networkQuery = new GetNetworkQuery(NetworkPartner);
            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<GetNetworkQuery>(), default))
                .ReturnsAsync(expectedNetwork)
                .Verifiable();

            var transactionIdQuery = new GetPaymentByTransactionIdQuery(TransactionId);
            Mock.Get(_mediator)
                .Setup(x => x.Send(It.Is<GetPaymentByTransactionIdQuery>(q => q.TransactionId == TransactionId), default))
                .ReturnsAsync(expectedTransaction)
                .Verifiable();

            Mock.Get(_userInfo)
                .Setup(x => x.IsAuthenticated)
                .Returns(true)
                .Verifiable();

            Mock.Get(_userInfo)
                .Setup(x => x.CanAccess(It.Is<string>(q => q == ClientCode), It.Is<string>(q => q == BeCode)))
                .Returns(true)
                .Verifiable();

            var transactionNetworkQuery = new GetNetworkByTransactionIdQuery(TransactionId);
            Mock.Get(_mediator)
                .Setup(x => x.Send(It.Is<GetNetworkByTransactionIdQuery>(q => q.TransactionId == TransactionId), default))
                .ReturnsAsync(expectedTransactionNetwork)
                .Verifiable();

            var payorEnrollmentQuery = new GetNetworkPayorEnrollmentQuery(NetworkPartner, ClientCode, BeCode);
            Mock.Get(_mediator)
                .Setup(x => x.Send(It.Is<GetNetworkPayorEnrollmentQuery>(q => q.NetworkName == NetworkPartner && q.ClientCode == ClientCode && q.BillingEntityCode == BeCode), default))
                .ReturnsAsync((NetworkPayorEnrollmentDto)null)
                .Verifiable();

            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            using (new AssertionScope())
            {
                var result = await act.Should()
                    .ThrowAsync<ItemNotFoundException>()
                    .WithMessage(ErrorMessage);
            }

            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenRestPaymentApiReturnsBadRequest_ThrowsValidationException()
        {
            var command = MockCommand();
            var expectedNetwork = MockNetwork();
            var expectedTransaction = MockTransaction();
            var expectedTransactionNetwork = MockTransactionNetwork();

            var expectedResult = new RejectPaymentResult
            (
                NetworkPartner,
                TransactionId,
                ResponseMessage
            );

            var networkQuery = new GetNetworkQuery(NetworkPartner);
            Mock.Get(_mediator)
                .Setup(x => x.Send(It.Is<GetNetworkQuery>(q => q.NetworkName == NetworkPartner), default))
                .ReturnsAsync(expectedNetwork)
                .Verifiable();

            var transactionIdQuery = new GetPaymentByTransactionIdQuery(TransactionId);
            Mock.Get(_mediator)
                .Setup(x => x.Send(It.Is<GetPaymentByTransactionIdQuery>(q => q.TransactionId == TransactionId), default))
                .ReturnsAsync(expectedTransaction)
                .Verifiable();

            Mock.Get(_userInfo)
                .Setup(x => x.IsAuthenticated)
                .Returns(true)
                .Verifiable();

            Mock.Get(_userInfo)
                .Setup(x => x.CanAccess(It.Is<string>(q => q == ClientCode), It.Is<string>(q => q == BeCode)))
                .Returns(true)
                .Verifiable();

            var transactionNetworkQuery = new GetNetworkByTransactionIdQuery(TransactionId);
            Mock.Get(_mediator)
                .Setup(x => x.Send(It.Is<GetNetworkByTransactionIdQuery>(q => q.TransactionId == TransactionId), default))
                .ReturnsAsync(expectedTransactionNetwork)
                .Verifiable();

            var expectedRejectionResponse = await MockRejectionApiResponse(HttpStatusCode.BadRequest);
            Mock.Get(_mediator)
                .Setup(x => x.Send(It.Is<RejectStpPayment.Request>(q => q.NetworkName == NetworkPartner && q.TransactionId == TransactionId), default))
                .ReturnsAsync(expectedRejectionResponse)
                .Verifiable();

            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            using (new AssertionScope())
            {
                var result = await act.Should()
                    .ThrowAsync<FluentValidation.ValidationException>()
                    .WithMessage(ResponseMessage);
            }

            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenRestPaymentApiReturnsUnauthorized_ThrowsNotAuthorizedException()
        {
            var command = MockCommand();
            var expectedNetwork = MockNetwork();
            var expectedTransaction = MockTransaction();
            var expectedTransactionNetwork = MockTransactionNetwork();

            var expectedResult = new RejectPaymentResult
            (
                NetworkPartner,
                TransactionId,
                ResponseMessage
            );

            var networkQuery = new GetNetworkQuery(NetworkPartner);
            Mock.Get(_mediator)
                .Setup(x => x.Send(It.Is<GetNetworkQuery>(q => q.NetworkName == NetworkPartner), default))
                .ReturnsAsync(expectedNetwork)
                .Verifiable();

            var transactionIdQuery = new GetPaymentByTransactionIdQuery(TransactionId);
            Mock.Get(_mediator)
                .Setup(x => x.Send(It.Is<GetPaymentByTransactionIdQuery>(q => q.TransactionId == TransactionId), default))
                .ReturnsAsync(expectedTransaction)
                .Verifiable();

            Mock.Get(_userInfo)
                .Setup(x => x.IsAuthenticated)
                .Returns(true)
                .Verifiable();

            Mock.Get(_userInfo)
                .Setup(x => x.CanAccess(It.Is<string>(q => q == ClientCode), It.Is<string>(q => q == BeCode)))
                .Returns(true)
                .Verifiable();

            var transactionNetworkQuery = new GetNetworkByTransactionIdQuery(TransactionId);
            Mock.Get(_mediator)
                .Setup(x => x.Send(It.Is<GetNetworkByTransactionIdQuery>(q => q.TransactionId == TransactionId), default))
                .ReturnsAsync(expectedTransactionNetwork)
                .Verifiable();

            const string ErrorMessage = "Client Id not authenticated.";
            var expectedRejectionResponse = await MockRejectionApiResponse(HttpStatusCode.Unauthorized);
            Mock.Get(_mediator)
                .Setup(x => x.Send(It.Is<RejectStpPayment.Request>(q => q.NetworkName == NetworkPartner && q.TransactionId == TransactionId), default))
                .ReturnsAsync(expectedRejectionResponse)
                .Verifiable();

            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            using (new AssertionScope())
            {
                var result = await act.Should()
                    .ThrowAsync<NotAuthorizedException>()
                    .WithMessage(ErrorMessage);
            }

            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenRestPaymentApiReturnsNotFound_ThrowsValidationException()
        {
            var command = MockCommand();
            var expectedNetwork = MockNetwork();
            var expectedTransaction = MockTransaction();
            var expectedTransactionNetwork = MockTransactionNetwork();

            var expectedResult = new RejectPaymentResult
            (
                NetworkPartner,
                TransactionId,
                ResponseMessage
            );

            var networkQuery = new GetNetworkQuery(NetworkPartner);
            Mock.Get(_mediator)
                .Setup(x => x.Send(It.Is<GetNetworkQuery>(q => q.NetworkName == NetworkPartner), default))
                .ReturnsAsync(expectedNetwork)
                .Verifiable();

            var transactionIdQuery = new GetPaymentByTransactionIdQuery(TransactionId);
            Mock.Get(_mediator)
                .Setup(x => x.Send(It.Is<GetPaymentByTransactionIdQuery>(q => q.TransactionId == TransactionId), default))
                .ReturnsAsync(expectedTransaction)
                .Verifiable();

            Mock.Get(_userInfo)
                .Setup(x => x.IsAuthenticated)
                .Returns(true)
                .Verifiable();

            Mock.Get(_userInfo)
                .Setup(x => x.CanAccess(It.Is<string>(q => q == ClientCode), It.Is<string>(q => q == BeCode)))
                .Returns(true)
                .Verifiable();

            var transactionNetworkQuery = new GetNetworkByTransactionIdQuery(TransactionId);
            Mock.Get(_mediator)
                .Setup(x => x.Send(It.Is<GetNetworkByTransactionIdQuery>(q => q.TransactionId == TransactionId), default))
                .ReturnsAsync(expectedTransactionNetwork)
                .Verifiable();

            const string ErrorMessage = "Invalid Transaction Id.";
            var expectedRejectionResponse = await MockRejectionApiResponse(HttpStatusCode.NotFound);
            Mock.Get(_mediator)
                .Setup(x => x.Send(It.Is<RejectStpPayment.Request>(q => q.NetworkName == NetworkPartner && q.TransactionId == TransactionId), default))
                .ReturnsAsync(expectedRejectionResponse)
                .Verifiable();

            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            using (new AssertionScope())
            {
                var result = await act.Should()
                    .ThrowAsync<ItemNotFoundException>()
                    .WithMessage(ErrorMessage);
            }

            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenRestPaymentApiReturnsServerError_ThrowsInvalidOperationException()
        {
            var command = MockCommand();
            var expectedNetwork = MockNetwork();
            var expectedTransaction = MockTransaction();
            var expectedTransactionNetwork = MockTransactionNetwork();

            var expectedResult = new RejectPaymentResult
            (
                NetworkPartner,
                TransactionId,
                ResponseMessage
            );

            var networkQuery = new GetNetworkQuery(NetworkPartner);
            Mock.Get(_mediator)
                .Setup(x => x.Send(It.Is<GetNetworkQuery>(q => q.NetworkName == NetworkPartner), default))
                .ReturnsAsync(expectedNetwork)
                .Verifiable();

            var transactionIdQuery = new GetPaymentByTransactionIdQuery(TransactionId);
            Mock.Get(_mediator)
                .Setup(x => x.Send(It.Is<GetPaymentByTransactionIdQuery>(q => q.TransactionId == TransactionId), default))
                .ReturnsAsync(expectedTransaction)
                .Verifiable();

            Mock.Get(_userInfo)
                .Setup(x => x.IsAuthenticated)
                .Returns(true)
                .Verifiable();

            Mock.Get(_userInfo)
                .Setup(x => x.CanAccess(It.Is<string>(q => q == ClientCode), It.Is<string>(q => q == BeCode)))
                .Returns(true)
                .Verifiable();

            var transactionNetworkQuery = new GetNetworkByTransactionIdQuery(TransactionId);
            Mock.Get(_mediator)
                .Setup(x => x.Send(It.Is<GetNetworkByTransactionIdQuery>(q => q.TransactionId == TransactionId), default))
                .ReturnsAsync(expectedTransactionNetwork)
                .Verifiable();

            const string ErrorMessage = "ServiceUnavailable";
            var expectedRejectionResponse = await MockRejectionApiResponse(HttpStatusCode.ServiceUnavailable);
            Mock.Get(_mediator)
                .Setup(x => x.Send(It.Is<RejectStpPayment.Request>(q => q.NetworkName == NetworkPartner && q.TransactionId == TransactionId), default))
                .ReturnsAsync(expectedRejectionResponse)
                .Verifiable();

            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            using (new AssertionScope())
            {
                var result = await act.Should()
                    .ThrowAsync<InvalidOperationException>()
                    .WithMessage(ErrorMessage);
            }

            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenRequestIsValid_ReturnsResult()
        {
            var command = MockCommand();
            var expectedNetwork = MockNetwork();
            var expectedTransaction = MockTransaction();
            var expectedTransactionNetwork = MockTransactionNetwork();

            var expectedResult = new RejectPaymentResult
            (
                NetworkPartner,
                TransactionId,
                ResponseMessage
            );

            var networkQuery = new GetNetworkQuery(NetworkPartner);
            Mock.Get(_mediator)
                .Setup(x => x.Send(It.Is<GetNetworkQuery>(q => q.NetworkName == NetworkPartner), default))
                .ReturnsAsync(expectedNetwork)
                .Verifiable();

            var transactionIdQuery = new GetPaymentByTransactionIdQuery(TransactionId);
            Mock.Get(_mediator)
                .Setup(x => x.Send(It.Is<GetPaymentByTransactionIdQuery>(q => q.TransactionId == TransactionId), default))
                .ReturnsAsync(expectedTransaction)
                .Verifiable();

            Mock.Get(_userInfo)
                .Setup(x => x.IsAuthenticated)
                .Returns(true)
                .Verifiable();

            Mock.Get(_userInfo)
                .Setup(x => x.CanAccess(It.Is<string>(q => q == ClientCode), It.Is<string>(q => q == BeCode)))
                .Returns(true)
                .Verifiable();

            var transactionNetworkQuery = new GetNetworkByTransactionIdQuery(TransactionId);
            Mock.Get(_mediator)
                .Setup(x => x.Send(It.Is<GetNetworkByTransactionIdQuery>(q => q.TransactionId == TransactionId), default))
                .ReturnsAsync(expectedTransactionNetwork)
                .Verifiable();

            var expectedRejectionResponse = await MockRejectionApiResponse(HttpStatusCode.OK);
            Mock.Get(_mediator)
                .Setup(x => x.Send(It.Is<RejectStpPayment.Request>(q => q.TransactionId == TransactionId), default))
                .ReturnsAsync(expectedRejectionResponse)
                .Verifiable();

            var result = await _handler.Handle(command, default);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Should().BeEquivalentTo(expectedResult);
            }

            Mock.Get(_mediator).VerifyAll();
        }
    }

    private static RejectPaymentCommand MockCommand(bool verifyEnrollment = false)
    {
        var command = new RejectPaymentCommand
        (
            NetworkPartner,
            TransactionId,
            RejectionCode,
            RejectionReason,
            verifyEnrollment
        );
        return command;
    }

    protected static NetworkDto MockNetwork(string active = "A")
    {
        return new NetworkDto()
        {
            NetworkId = 1,
            NetworkPartner = NetworkPartner,
            NetworkType = "STP",
            ActiveStatus = active
        };
    }

    private static TransactionPayment MockTransaction()
    {
        var payment = new TransactionPayment
        {
            Id = Id,
            TransactionId = TransactionId,
            ClientCode = ClientCode,
            BillingEntityCode = BeCode,
            PaymentId = PaymentId
        };
        return payment;
    }

    private static TransactionNetwork MockTransactionNetwork()
    {
        var network = new TransactionNetwork
        {
            Id = Id,
            TransactionId = TransactionId,
            NetworkName = NetworkPartner,
            EnrollmentType = EnrollmentType,
            NetworkType = NetworkType
        };
        return network;
    }

    private static TransactionNetwork MockOtherTransactionNetwork()
    {
        var network = new TransactionNetwork
        {
            Id = Id,
            TransactionId = TransactionId,
            NetworkName = "OTHER NETWORK",
            EnrollmentType = EnrollmentType,
            NetworkType = NetworkType
        };
        return network;
    }

    private static async Task<IApiResponse<RejectStpPayment.Response>> MockRejectionApiResponse(HttpStatusCode statusCode)
    {
        var response = new RejectStpPayment.Response(string.Empty, ResponseMessage, string.Empty, string.Empty, string.Empty, ResponseMessage);
        var apiResponse = Mock.Of<IApiResponse<RejectStpPayment.Response>>(MockBehavior.Strict);

        Mock.Get(apiResponse)
            .Setup(x => x.Content)
            .Returns(response);

        Mock.Get(apiResponse)
            .Setup(x => x.StatusCode)
            .Returns(statusCode);

        Mock.Get(apiResponse)
            .Setup(x => x.IsSuccessStatusCode)
            .Returns(statusCode == HttpStatusCode.OK);

        if(statusCode != HttpStatusCode.OK && statusCode != HttpStatusCode.BadRequest)
        {
            var apiException = await ApiException.Create(
                statusCode.ToString(),
                new HttpRequestMessage(),
                HttpMethod.Get,
                new HttpResponseMessage(),
                null);

            Mock.Get(apiResponse)
                .Setup(x => x.Error)
                .Returns(apiException);
        }

        return apiResponse;
    }
}
