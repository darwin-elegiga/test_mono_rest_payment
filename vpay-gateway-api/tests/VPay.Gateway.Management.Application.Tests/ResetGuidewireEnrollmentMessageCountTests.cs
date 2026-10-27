using FluentAssertions;
using FluentAssertions.Execution;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VPay.Gateway.Common.Guidewire.Models;
using VPay.Gateway.Data.Sql.Exceptions;
using VPay.Gateway.Data.Sql.PepClaims;
using VPay.Gateway.Management.Application.GuidewirePayments;
using Xunit;

namespace VPay.Gateway.Management.Application.Tests;

/// <summary>
/// Tests for <see cref="ResetGuidewireEnrollmentMessageCount"/>
/// </summary>
public class ResetGuidewireEnrollmentMessageCountTests
{
    private readonly IMediator _mediator;
    private readonly ILogger<ResetGuidewireEnrollmentMessageCount.Handler> _logger;
    private readonly ResetGuidewireEnrollmentMessageCount.Handler _handler;

    private const string CurrentProcessStatus = "SENDING";

    public ResetGuidewireEnrollmentMessageCountTests()
    {
        _mediator = Mock.Of<IMediator>(MockBehavior.Strict);
        _logger = new NullLogger<ResetGuidewireEnrollmentMessageCount.Handler>();
        _handler = new ResetGuidewireEnrollmentMessageCount.Handler(_mediator, _logger);
    }

    public class Constructor : ResetGuidewireEnrollmentMessageCountTests
    {
        [Fact]
        public void WhenMediatorIsNull_ThrowsArgumentNullException()
        {
            Action act = new(() =>
            {
                var handler = new ResetGuidewireEnrollmentMessageCount.Handler(null!, _logger);
            });
            act.Should().ThrowExactly<ArgumentNullException>()
                .Which.ParamName.Should().Be("mediator");
        }

        [Fact]
        public void WhenLoggerIsNull_ThrowsException()
        {
            Action act = new(() =>
            {
                var handler = new ResetGuidewireEnrollmentMessageCount.Handler(_mediator, null!);
            });
            act.Should().ThrowExactly<ArgumentNullException>()
                .Which.ParamName.Should().Be("logger");
        }
    }

    public class Handle : ResetGuidewireEnrollmentMessageCountTests
    {
        [Fact]
        public async Task WhenRequestIsNull_ThrowsException()
        {
            Func<Task> act = async () => await _handler.Handle(null, CancellationToken.None);
            var result = await act.Should().ThrowAsync<ArgumentNullException>();
            result.Which.ParamName.Should().Be("request");
        }

        [Fact]
        public async Task WhenRequestThrowsError_ThrowsException()
        {
            const string ClientCode = "ABC";
            const string MemberId = "M001";
            const string ClaimId = "E001";
            const int RetryCount = 2;
            const string ErrorMessage = "Some error";

            var expectedException = new InvalidOperationException(ErrorMessage);

            ResetGuidewireEnrollmentMessageCount.Command request = new(ClientCode, MemberId, ClaimId, RetryCount);

            var payeeStatusList = MockPayeeStatusList(request.ClaimId, 2);

            EnrollmentStatusRetryCountUpdate.Command command = new(ClientCode, MemberId, ClaimId, RetryCount);

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny< EnrollmentStatusRetryCountUpdate.Command>(), default))
                .ThrowsAsync(expectedException)
                .Verifiable("Send Update Payee Status Retry Count Command");

            Func<Task> act = async () => await _handler.Handle(request, CancellationToken.None);
            var result = await act.Should().ThrowAsync<InvalidOperationException>();
            result.WithMessage(ErrorMessage);

            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenRequestReturnsNull_ThrowsException()
        {
            const string ClientCode = "ABC";
            const string MemberId = "M001";
            const string ClaimId = "E001";
            const int RetryCount = 2;
            const string ErrorMessage = "Unable to reset message retry count of Guidewire enrollment message(s)";

            ResetGuidewireEnrollmentMessageCount.Command request = new(ClientCode, MemberId, ClaimId, RetryCount);

            var items = MockPayeeStatusList(request.ClaimId, 2);

            EnrollmentStatusRetryCountUpdate.Command command = new(ClientCode, MemberId, ClaimId, RetryCount);

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny< EnrollmentStatusRetryCountUpdate.Command>(), default))
                .ReturnsAsync((PayeeStatus)null)
                .Verifiable("Send Update Payee Status Retry Count Command");

            Func<Task> act = async () => await _handler.Handle(request, CancellationToken.None);
            var result = await act.Should().ThrowAsync<UpdateDidNotOccurException>();
            result.WithMessage(ErrorMessage);

            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenRequestMatchesItem_ReturnsSuccess()
        {
            const string ClientCode = "ABC";
            const string MemberId = "M001";
            const string ClaimId = "E001";
            const int RetryCount = 2;

            PayeeStatus expectedResult = new()
            {
                Id = 1,
                ClaimId = ClaimId,
                CurrentProcessStatus = CurrentProcessStatus
            };

            ResetGuidewireEnrollmentMessageCount.Command request = new(ClientCode, MemberId, ClaimId, RetryCount);

            var successMessage = $"Successfully reset Guidewire enrollment message retry count to {request.RetryCount} (client {request.ClientCode}, member {request.MemberId}, claim {request.ClaimId})";
            GuidewireResult guidewireResult = new() { Message = successMessage, Status = CurrentProcessStatus };

            EnrollmentStatusRetryCountUpdate.Command command = new(ClientCode, MemberId, ClaimId, RetryCount);

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny< EnrollmentStatusRetryCountUpdate.Command>(), default))
                .ReturnsAsync(expectedResult)
                .Verifiable("Send Update Payee Status Retry Count Command");

            var result = await _handler.Handle(request, CancellationToken.None);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Should().BeEquivalentTo(guidewireResult);
            }

            Mock.Get(_mediator).VerifyAll();
        }

        private static System.Collections.ObjectModel.ReadOnlyCollection<PayeeStatus> MockPayeeStatusList(string claimId, int count)
        {
            var list = new List<PayeeStatus>();
            for (int i = 0; i < count; i++)
            {
                list.Add(new PayeeStatus()
                {
                    Id = i + 1,
                    ClaimId = claimId,
                    CurrentProcessStatus = CurrentProcessStatus,
                });
            }
            return list.AsReadOnly();
        }
    }
}
