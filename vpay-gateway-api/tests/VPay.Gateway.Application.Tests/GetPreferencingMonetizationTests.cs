using FluentAssertions;
using Moq;
using VPay.Gateway.Application;
using VPay.Gateway.Common.Preferencing;
using VPay.Gateway.Common.Preferencing.Models;
using Xunit;

namespace VPay.Gateway.Application.Tests;

public class GetPreferencingMonetizationTests : IDisposable
{
    private readonly Mock<IPreferencingApi> _preferencingApiMock = new(MockBehavior.Strict);
    private readonly GetPreferencingMonetization.Handler _handler;

    public GetPreferencingMonetizationTests() =>
        _handler = new GetPreferencingMonetization.Handler(_preferencingApiMock.Object);

    public void Dispose()
    {
        _preferencingApiMock.VerifyAll();
        GC.SuppressFinalize(this);
    }

    public class Constructor : GetPreferencingMonetizationTests
    {
        [Fact]
        public void WhenPreferencingApiIsNull_ThrowsArgumentNullException() =>
            new Action(() => _ = new GetPreferencingMonetization.Handler(null!))
                .Should().ThrowExactly<ArgumentNullException>()
                .And.ParamName.Should().Be("preferencingApi");
    }

    public class Handle : GetPreferencingMonetizationTests
    {
        [Fact]
        public async Task WhenPreferencingApiReturnsResult_MapsResponseToMonetizationResult()
        {
            var request = new MonetizationRequest
            {
                CorrelationID = "request-correlation-id",
                OriginCode = "ORIGIN",
                ClientCode = "CLIENT",
                ProviderType = "MEDICAL",
                Date = DateTime.UtcNow,
            };

            var response = new MonetizationResponse
            {
                CorrelationID = "response-correlation-id",
                Monetized = true,
            };

            _preferencingApiMock
                .Setup(x => x.GetMonetizedStatus(request, CancellationToken.None))
                .ReturnsAsync(response)
                .Verifiable();

            var query = new GetPreferencingMonetization.Query(request);

            var result = await _handler.Handle(query, CancellationToken.None).ConfigureAwait(false);

            result.Should().NotBeNull();
            result.CorrelationId.Should().Be(response.CorrelationID);
            result.ValidatedPayee.Should().BeTrue();
        }

        [Fact]
        public async Task WhenOperationIsCanceled_ThrowsOperationCanceledException()
        {
            var request = new MonetizationRequest
            {
                CorrelationID = "request-correlation-id",
                OriginCode = "ORIGIN",
                ClientCode = "CLIENT",
                ProviderType = "MEDICAL",
                Date = DateTime.UtcNow,
            };

            _preferencingApiMock
                .Setup(x => x.GetMonetizedStatus(request, CancellationToken.None))
                .ThrowsAsync(new OperationCanceledException())
                .Verifiable();

            var query = new GetPreferencingMonetization.Query(request);

            Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None).ConfigureAwait(false);

            await act.Should().ThrowExactlyAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task WhenUnexpectedExceptionOccurs_ThrowsInvalidOperationExceptionWithInnerException()
        {
            var request = new MonetizationRequest
            {
                CorrelationID = "request-correlation-id",
                OriginCode = "ORIGIN",
                ClientCode = "CLIENT",
                ProviderType = "MEDICAL",
                Date = DateTime.UtcNow,
            };

            var originalException = new Exception("boom");

            _preferencingApiMock
                .Setup(x => x.GetMonetizedStatus(request, CancellationToken.None))
                .ThrowsAsync(originalException)
                .Verifiable();

            var query = new GetPreferencingMonetization.Query(request);

            Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None).ConfigureAwait(false);

            var assertion = await act.Should().ThrowExactlyAsync<InvalidOperationException>();
            assertion.Which.Message.Should().Be("An error occurred while processing the monetization request.");
            assertion.Which.InnerException.Should().BeSameAs(originalException);
        }
    }
}
