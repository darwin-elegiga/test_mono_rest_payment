using FluentAssertions;
using FluentAssertions.Execution;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VPay.Gateway.Data.Paging;
using VPay.Gateway.Data.Sql.PepClaims;
using VPay.Gateway.Management.Application.GuidewirePayments;
using Xunit;

namespace VPay.Gateway.Management.Application.Tests;

/// <summary>
/// Tests for <see cref="SearchGuidewireEnrollmentMessages"/>
/// </summary>
public class SearchGuidewireEnrollmentMessagesTests
{
    private readonly IMediator _mediator;
    private readonly ILogger<SearchGuidewireEnrollmentMessages.Handler> _logger;
    private readonly SearchGuidewireEnrollmentMessages.Handler _handler;

    public SearchGuidewireEnrollmentMessagesTests()
    {
        _mediator = Mock.Of<IMediator>(MockBehavior.Strict);
        _logger = new NullLogger<SearchGuidewireEnrollmentMessages.Handler>();
        _handler = new SearchGuidewireEnrollmentMessages.Handler(_mediator, _logger);
    }

    public class Constructor : SearchGuidewireEnrollmentMessagesTests
    {
        [Fact]
        public void WhenMediatorIsNull_ThrowsArgumentNullException()
        {
            Action act = new(() =>
            {
                var handler = new SearchGuidewireEnrollmentMessages.Handler(null!, _logger);
            });
            act.Should().ThrowExactly<ArgumentNullException>()
                .Which.ParamName.Should().Be("mediator");
        }

        [Fact]
        public void WhenLoggerIsNull_ThrowsException()
        {
            Action act = new(() =>
            {
                var handler = new SearchGuidewireEnrollmentMessages.Handler(_mediator, null!);
            });
            act.Should().ThrowExactly<ArgumentNullException>()
                .Which.ParamName.Should().Be("logger");
        }
    }

    public class Handle : SearchGuidewireEnrollmentMessagesTests
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

            SearchGuidewireEnrollmentMessages.Query request = new();
            EnrollmentStatusSearch.Query query = new(request.ProcessStatus, request.ClientCode, request.MemberId, request.ClaimId, request.Page, request.PageSize);

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.IsAny<EnrollmentStatusSearch.Query>(), default))
                .ThrowsAsync(expectedException)
                .Verifiable();

            Func<Task> act = async () => await _handler.Handle(request, CancellationToken.None);
            var result = await act.Should().ThrowAsync<InvalidOperationException>();
            result.WithMessage(ErrorMessage);
        }

        [Fact]
        public async Task WhenRequestMatchesNoItems_ReturnsEmptyList()
        {
            List<EnrollmentStatus> items = new();

            SearchGuidewireEnrollmentMessages.Query request = new();
            EnrollmentStatusSearch.Query query = new(request.ProcessStatus, request.ClientCode, request.MemberId, request.ClaimId, request.Page, request.PageSize);
            PagedResult<EnrollmentStatus> expectedResult = new(items, request.Page.Value, request.PageSize.Value, items.Count);

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.Is<EnrollmentStatusSearch.Query>(q => q.Equals(query)), default))
                .ReturnsAsync(expectedResult)
                .Verifiable();

            var result = await _handler.Handle(request, CancellationToken.None);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Should().BeEquivalentTo(expectedResult);
            }

            Mock.Get(_mediator).VerifyAll();
        }

        [Fact]
        public async Task WhenRequestMatchesItems_ReturnsItemList()
        {
            List<EnrollmentStatus> items = new()
            {
                new EnrollmentStatus { Id = 1, ClientCode = "ABC", MemberId = "ABC1", ClaimId = "E01", CurrentProcessStatus = "SENDING"},
                new EnrollmentStatus { Id = 3, ClientCode = "ABC", MemberId = "ABC1", ClaimId = "E02", CurrentProcessStatus = "SENDING"}
            };

            SearchGuidewireEnrollmentMessages.Query request = new();
            EnrollmentStatusSearch.Query query = new(request.ProcessStatus, request.ClientCode, request.MemberId, request.ClaimId, request.Page, request.PageSize);
            PagedResult<EnrollmentStatus> expectedResult = new(items, request.Page.Value, request.PageSize.Value, items.Count);

            Mock.Get(_mediator)
                .Setup(x => x.Send(It.Is<EnrollmentStatusSearch.Query>(q => q.Equals(query)), default))
                .ReturnsAsync(expectedResult)
                .Verifiable();

            var result = await _handler.Handle(request, CancellationToken.None);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Should().BeEquivalentTo(expectedResult);
            }

            Mock.Get(_mediator).VerifyAll();
        }
    }
}
