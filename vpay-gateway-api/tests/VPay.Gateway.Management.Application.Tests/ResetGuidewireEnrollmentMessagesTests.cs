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
/// Tests for <see cref="ResetGuidewireEnrollmentMessages"/>
/// </summary>
public class ResetGuidewireEnrollmentMessagesTests
{
    private readonly IMediator _mediator;
    private readonly ILogger<ResetGuidewireEnrollmentMessages.Handler> _logger;
    private readonly ResetGuidewireEnrollmentMessages.Handler _handler;

    public ResetGuidewireEnrollmentMessagesTests()
    {
        _mediator = Mock.Of<IMediator>(MockBehavior.Strict);
        _logger = new NullLogger<ResetGuidewireEnrollmentMessages.Handler>();
        _handler = new ResetGuidewireEnrollmentMessages.Handler(_mediator, _logger);
    }

    public class Constructor : ResetGuidewireEnrollmentMessagesTests
    {
        [Fact]
        public void WhenMediatorIsNull_ThrowsArgumentNullException()
        {
            Action act = new(() =>
            {
                var handler = new ResetGuidewireEnrollmentMessages.Handler(null!, _logger);
            });
            act.Should().ThrowExactly<ArgumentNullException>()
                .Which.ParamName.Should().Be("mediator");
        }

        [Fact]
        public void WhenLoggerIsNull_ThrowsException()
        {
            Action act = new(() =>
            {
                var handler = new ResetGuidewireEnrollmentMessages.Handler(_mediator, null!);
            });
            act.Should().ThrowExactly<ArgumentNullException>()
                .Which.ParamName.Should().Be("logger");
        }
    }

    public class Handle : ResetGuidewireEnrollmentMessagesTests
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
            const string CurrentProcessStatus = "SENDING";
            const string NewProcessStatus = "SKIPPED";
            const string ErrorMessage = "Some error";

            var expectedException = new InvalidOperationException(ErrorMessage);

            ResetGuidewireEnrollmentMessages.Command request = new(ClientCode, MemberId, ClaimId, CurrentProcessStatus, NewProcessStatus);

            PayeeStatusesQuery query = new(request.ClientCode, request.MemberId);
            var payeeStatusList = MockPayeeStatusList(request.ClaimId, request.CurrentProcessStatus, 2);

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<PayeeStatusesQuery>(), default))
                .ReturnsAsync(payeeStatusList)
                .Verifiable("Send Payee Status Query");

            UpdatePayeeStatusCommand command = new(1, request.NewProcessStatus, request.CurrentProcessStatus);

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<UpdatePayeeStatusCommand>(), default))
                .ThrowsAsync(expectedException)
                .Verifiable("Send Update Payee Status Command");

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
            const string CurrentProcessStatus = "SENDING";
            const string NewProcessStatus = "SKIPPED";
            const string ErrorMessage = "Unable to reset processing status of Guidewire enrollment message(s)";

            ResetGuidewireEnrollmentMessages.Command request = new(ClientCode, MemberId, ClaimId, CurrentProcessStatus, NewProcessStatus);
            PayeeStatusesQuery query = new(request.ClientCode, request.MemberId);

            var items = MockPayeeStatusList(request.ClaimId, request.CurrentProcessStatus, 2);

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<PayeeStatusesQuery>(), default))
                .ReturnsAsync(items)
                .Verifiable("Send Payee Status Query");

            UpdatePayeeStatusCommand command = new(1, request.NewProcessStatus, request.CurrentProcessStatus);

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<UpdatePayeeStatusCommand>(), default))
                .ReturnsAsync((PayeeStatus)null)
                .Verifiable("Send Update Payee Status Command");

            Func<Task> act = async () => await _handler.Handle(request, CancellationToken.None);
            var result = await act.Should().ThrowAsync<UpdateDidNotOccurException>();
            result.WithMessage(ErrorMessage);

            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenRequestMatchesNoItems_ReturnsError()
        {
            const string ClientCode = "ABC";
            const string MemberId = "M001";
            const string ClaimId = "E001";
            const string CurrentProcessStatus = "SENDING";
            const string NewProcessStatus = "SKIPPED";

            ResetGuidewireEnrollmentMessages.Command request = new(ClientCode, MemberId, ClaimId, CurrentProcessStatus, NewProcessStatus);
            PayeeStatusesQuery query = new(request.ClientCode, request.MemberId);

            var items = MockPayeeStatusList(request.ClaimId, request.CurrentProcessStatus, 0);
            var errorMessage = $"Record not found for Client '{request.ClientCode}', Member Id '{request.MemberId}', Claim Id '{request.ClaimId}'";
            GuidewireResult guidewireResult = new() { Message = errorMessage, Status = request.NewProcessStatus };

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<PayeeStatusesQuery>(), default))
                .ReturnsAsync(items)
                .Verifiable("Send Payee Status Query");

            var result = await _handler.Handle(request, CancellationToken.None);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Should().BeEquivalentTo(guidewireResult);
            }

            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenRequestMatchesItems_ReturnsSuccess()
        {
            const string ClientCode = "ABC";
            const string MemberId = "M001";
            const string ClaimId = "E001";
            const string CurrentProcessStatus = "SENDING";
            const string NewProcessStatus = "SKIPPED";

            PayeeStatus expectedResult = new()
            {
                Id = 1,
                ClaimId = ClaimId,
                CurrentProcessStatus = CurrentProcessStatus
            };

            ResetGuidewireEnrollmentMessages.Command request = new(ClientCode, MemberId, ClaimId, CurrentProcessStatus, NewProcessStatus);
            PayeeStatusesQuery query = new(request.ClientCode, request.MemberId);

            var items = MockPayeeStatusList(request.ClaimId, request.CurrentProcessStatus, 2);
            var successMessage = $"Successfully reset Guidewire enrollment message to {request.NewProcessStatus} status (client {request.ClientCode}, member {request.MemberId}, claim {request.ClaimId})";
            GuidewireResult guidewireResult = new() { Message = successMessage, Status = request.NewProcessStatus };

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<PayeeStatusesQuery>(), default))
                .ReturnsAsync(items)
                .Verifiable("Send Payee Status Query");

            UpdatePayeeStatusCommand command = new(1, request.NewProcessStatus, request.CurrentProcessStatus);

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<UpdatePayeeStatusCommand>(), default))
                .ReturnsAsync(expectedResult)
                .Verifiable("Send Update Payee Status Command");

            var result = await _handler.Handle(request, CancellationToken.None);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Should().BeEquivalentTo(guidewireResult);
            }

            Mock.Get(_mediator).VerifyAll();
        }

        private static System.Collections.ObjectModel.ReadOnlyCollection<PayeeStatus> MockPayeeStatusList(string claimId, string currentProcessStatus, int count)
        {
            var list = new List<PayeeStatus>();
            for (int i = 0; i < count; i++)
            {
                list.Add(new PayeeStatus()
                {
                    Id = i + 1,
                    ClaimId = claimId,
                    CurrentProcessStatus = currentProcessStatus,
                });
            }
            return list.AsReadOnly();
        }
    }
}
