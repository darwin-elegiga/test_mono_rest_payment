using FluentAssertions;
using FluentAssertions.Execution;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VPay.Gateway.ClientProcessor.Rabbit;
using VPay.Gateway.Common.Guidewire.Models;
using VPay.Gateway.Data.Sql.PepClaims;
using VPay.Gateway.Management.Application.GuidewirePayments;
using Xunit;

namespace VPay.Gateway.Management.Application.Tests;

/// <summary>
/// Tests for <see cref="ReplayGuidewireEnrollmentMessages"/>
/// </summary>
public class ReplayGuidewireEnrollmentMessagesTests
{
    private readonly IMediator _mediator;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<ReplayGuidewireEnrollmentMessages.Handler> _logger;
    private readonly ReplayGuidewireEnrollmentMessages.Handler _handler;

    public ReplayGuidewireEnrollmentMessagesTests()
    {
        _mediator = Mock.Of<IMediator>(MockBehavior.Strict);
        _publishEndpoint = Mock.Of<IPublishEndpoint>(MockBehavior.Strict);
        _logger = new NullLogger<ReplayGuidewireEnrollmentMessages.Handler>();
        _handler = new ReplayGuidewireEnrollmentMessages.Handler(_mediator, _publishEndpoint, _logger);
    }

    public class Constructor : ReplayGuidewireEnrollmentMessagesTests
    {
        [Fact]
        public void WhenMediatorIsNull_ThrowsArgumentNullException()
        {
            Action act = new(() =>
            {
                var handler = new ReplayGuidewireEnrollmentMessages.Handler(null!, _publishEndpoint, _logger);
            });
            act.Should().ThrowExactly<ArgumentNullException>()
                .Which.ParamName.Should().Be("mediator");
        }

        [Fact]
        public void WhenPublishEndpointIsNull_ThrowsArgumentNullException()
        {
            Action act = new(() =>
            {
                var handler = new ReplayGuidewireEnrollmentMessages.Handler(_mediator, null!, _logger);
            });
            act.Should().ThrowExactly<ArgumentNullException>()
                .Which.ParamName.Should().Be("publishEndpoint");
        }

        [Fact]
        public void WhenLoggerIsNull_ThrowsException()
        {
            Action act = new(() =>
            {
                var handler = new ReplayGuidewireEnrollmentMessages.Handler(_mediator, _publishEndpoint, null!);
            });
            act.Should().ThrowExactly<ArgumentNullException>()
                .Which.ParamName.Should().Be("logger");
        }
    }

    public class Handle : ReplayGuidewireEnrollmentMessagesTests
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
            const string ErrorMessage = "Some error";
            var expectedException = new InvalidOperationException(ErrorMessage);

            ReplayGuidewireEnrollmentMessages.Command request = new();
            EnrollmentStatusUpdate.Command query = new
            (
                request.CurrentProcessStatus,
                request.NewProcessStatus,
                request.ClientCode,
                request.MemberId,
                request.ClaimId,
                request.MessageCount,
                request.MinMessageAgeInMinutes,
                request.MaxMessageAgeInDays
            );

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<EnrollmentStatusUpdate.Command>(), default))
                .ThrowsAsync(expectedException)
                .Verifiable();

            Func<Task> act = async () => await _handler.Handle(request, CancellationToken.None);
            var result = await act.Should().ThrowAsync<InvalidOperationException>();
            result.WithMessage(ErrorMessage);
        }

        [Fact]
        public async Task WhenRequestMatchesNoItems_ReturnsEmptyList()
        {
            List<EnrollmentStatus> items = [];
            const string SuccessMessage = "No Guidewire enrollment message(s) found to reset to SCHEDULED status";

            ReplayGuidewireEnrollmentMessages.Command request = new();
            EnrollmentStatusUpdate.Command query = new
            (
                request.CurrentProcessStatus,
                request.NewProcessStatus,
                request.ClientCode,
                request.MemberId,
                request.ClaimId,
                request.MessageCount,
                request.MinMessageAgeInMinutes,
                request.MaxMessageAgeInDays
            );
            List<EnrollmentStatus> expectedResult = [.. items];
            GuidewireResult guidewireResult = new() { Message = SuccessMessage, Status = request.NewProcessStatus };

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.Is<EnrollmentStatusUpdate.Command>(q => q.Equals(query)), default))
                .ReturnsAsync(expectedResult)
                .Verifiable();

            var result = await _handler.Handle(request, CancellationToken.None);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Should().BeEquivalentTo(guidewireResult);
            }

            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenRequestMatchesSingleItem_ReturnsItem()
        {
            List<EnrollmentStatus> items =
            [
                new EnrollmentStatus { Id = 1, ClientCode = "ABC", MemberId = "ABC1", ClaimId = "E01", CurrentProcessStatus = "SENDING",}
            ];
            var claims = string.Join(',', items.Select(x => x.ClaimId));
            var successMessage = $"Successfully reset {items.Count} Guidewire enrollment message(s) to SCHEDULED status ({claims})";

            ReplayGuidewireEnrollmentMessages.Command request = new();
            EnrollmentStatusUpdate.Command query = new
            (
                request.CurrentProcessStatus,
                request.NewProcessStatus,
                request.ClientCode,
                request.MemberId,
                request.ClaimId,
                request.MessageCount,
                request.MinMessageAgeInMinutes,
                request.MaxMessageAgeInDays
            );
            List<EnrollmentStatus> expectedResult = [.. items];
            GuidewireResult guidewireResult = new() { Message = successMessage, Status = request.NewProcessStatus };

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.Is<EnrollmentStatusUpdate.Command>(q => q.Equals(query)), default))
                .ReturnsAsync(expectedResult)
                .Verifiable();

            Mock.Get(_publishEndpoint).Setup(x => x.Publish(It.Is<ProcessEnrollmentStatusRecords>(r => r.ClientCode == "ABC" && r.MemberId == "ABC1"), default))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var result = await _handler.Handle(request, CancellationToken.None);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Should().BeEquivalentTo(guidewireResult);
            }

            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenRequestMatchesItems_ReturnsItemList()
        {
            List<EnrollmentStatus> items =
            [
                new EnrollmentStatus { Id = 1, ClientCode = "ABC", MemberId = "ABC1", ClaimId = "E01", CurrentProcessStatus = "SENDING",},
                new EnrollmentStatus { Id = 3, ClientCode = "ABC", MemberId = "ABC2", ClaimId = "E02", CurrentProcessStatus = "SENDING"}
            ];
            var claims = string.Join(',', items.Select(x => x.ClaimId));
            var successMessage = $"Successfully reset {items.Count} Guidewire enrollment message(s) to SCHEDULED status ({claims})";

            ReplayGuidewireEnrollmentMessages.Command request = new();
            EnrollmentStatusUpdate.Command query = new
            (
                request.CurrentProcessStatus,
                request.NewProcessStatus,
                request.ClientCode,
                request.MemberId,
                request.ClaimId,
                request.MessageCount,
                request.MinMessageAgeInMinutes,
                request.MaxMessageAgeInDays
            );
            List<EnrollmentStatus> expectedResult = [.. items];
            GuidewireResult guidewireResult = new() { Message = successMessage, Status = request.NewProcessStatus };

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.Is<EnrollmentStatusUpdate.Command>(q => q.Equals(query)), default))
                .ReturnsAsync(expectedResult)
                .Verifiable();

            Mock.Get(_publishEndpoint).Setup(x => x.Publish(It.Is<ProcessEnrollmentStatusRecords>(r => r.ClientCode == "ABC" && r.MemberId == "ABC1"), default))
                .Returns(Task.CompletedTask)
                .Verifiable();

            Mock.Get(_publishEndpoint).Setup(x => x.Publish(It.Is<ProcessEnrollmentStatusRecords>(r => r.ClientCode == "ABC" && r.MemberId == "ABC2"), default))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var result = await _handler.Handle(request, CancellationToken.None);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Should().BeEquivalentTo(guidewireResult);
            }

            Mock.Get(_mediator).VerifyAll();
        }
    }
}
