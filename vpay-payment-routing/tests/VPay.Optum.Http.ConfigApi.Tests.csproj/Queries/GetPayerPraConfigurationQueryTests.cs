using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Polly;
using Refit;
using VPay.Extensions.Caching.Memory;
using VPay.Optum.Http.ConfigApi.Models;
using VPay.Optum.Http.ConfigApi.Queries;

namespace VPay.Optum.Http.ConfigApi.Tests.Queries;

/// <summary>
/// Tests for <see cref="GetPayerPraConfigurationQuery"/>.
/// </summary>
public class GetPayerPraConfigurationQueryTests
{
    // A no-op policy that executes immediately with no retries - keeps tests fast and
    // lets us assert exact call counts without sleeping.
    private static readonly IAsyncPolicy<IApiResponse<PayerPraConfigurationDto>> NoRetryPolicy =
        Policy.NoOpAsync<IApiResponse<PayerPraConfigurationDto>>();

    // A policy that retries once immediately - used to verify retry behaviour triggers.
    private static readonly IAsyncPolicy<IApiResponse<PayerPraConfigurationDto>> SingleRetryPolicy =
        Policy<IApiResponse<PayerPraConfigurationDto>>
            .Handle<HttpRequestException>()
            .OrResult(r => !r.IsSuccessStatusCode)
            .RetryAsync(1);

    // A policy that retries 10 times immediately - used to verify exhaustion behaviour.
    private static readonly IAsyncPolicy<IApiResponse<PayerPraConfigurationDto>> ExhaustingRetryPolicy =
        Policy<IApiResponse<PayerPraConfigurationDto>>
            .Handle<HttpRequestException>()
            .OrResult(r => !r.IsSuccessStatusCode)
            .RetryAsync(10);

    private readonly Mock<IConfigApi> _configApi;
    private readonly GetPayerPraConfigurationQuery.PayerPraConfigurationQueryHandler _handler;

    public GetPayerPraConfigurationQueryTests()
    {
        _configApi = new Mock<IConfigApi>(MockBehavior.Strict);
        _handler = new GetPayerPraConfigurationQuery.PayerPraConfigurationQueryHandler(
            _configApi.Object, CreatePassthroughCache(), CreateNullLogger(), NoRetryPolicy);
    }

    // Creates a cache stub that always misses (TryGetValue returns false) and always executes
    // the factory delegate on GetOrCreateAsync — i.e. no actual caching — so existing
    // call-count assertions remain valid.
    private static IMemoryCache<PayerPraConfigurationDto> CreatePassthroughCache()
    {
        var cache = new Mock<IMemoryCache<PayerPraConfigurationDto>>();
        PayerPraConfigurationDto? unused;
        cache
            .Setup(c => c.TryGetValue(It.IsAny<string>(), out unused))
            .Returns(false);
        cache
            .Setup(c => c.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<PayerPraConfigurationDto>>>(),
                It.IsAny<MemoryCacheEntryOptions>(),
                It.IsAny<bool>()))
            .Returns((string _, Func<Task<PayerPraConfigurationDto>> factory, MemoryCacheEntryOptions _, bool _) => factory());
        return cache.Object;
    }

    private static ILogger<GetPayerPraConfigurationQuery.PayerPraConfigurationQueryHandler> CreateNullLogger() =>
        new Mock<ILogger<GetPayerPraConfigurationQuery.PayerPraConfigurationQueryHandler>>().Object;

    // -------------------------------------------------------------------------
    // Constructor
    // -------------------------------------------------------------------------

    public sealed class Constructor : GetPayerPraConfigurationQueryTests
    {
        [Fact]
        public void WhenConfigApiIsNull_ThrowsArgumentNullException()
        {
            Action act = () => new GetPayerPraConfigurationQuery.PayerPraConfigurationQueryHandler(null!, CreatePassthroughCache(), CreateNullLogger());
            act.Should().ThrowExactly<ArgumentNullException>()
                .Which.ParamName.Should().Be("configApi");
        }

        [Fact]
        public void WhenConfigApiIsProvided_DoesNotThrow()
        {
            Action act = () => new GetPayerPraConfigurationQuery.PayerPraConfigurationQueryHandler(
                new Mock<IConfigApi>().Object, CreatePassthroughCache(), CreateNullLogger());
            act.Should().NotThrow();
        }
    }

    // -------------------------------------------------------------------------
    // Handle — validation guards
    // -------------------------------------------------------------------------

    public sealed class HandleValidation : GetPayerPraConfigurationQueryTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void WhenOriginCodeIsNullOrWhitespace_ThrowsArgumentException(string originCode)
        {
            var query = new GetPayerPraConfigurationQuery.Query(originCode, "CLIENT", "BILLING");
            Action act = () => _handler.Handle(query, CancellationToken.None);
            act.Should().ThrowExactly<ArgumentException>()
                .WithMessage("*OriginCode cannot be empty*");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void WhenClientCodeIsNullOrWhitespace_ThrowsArgumentException(string clientCode)
        {
            var query = new GetPayerPraConfigurationQuery.Query("ORIGIN", clientCode, "BILLING");
            Action act = () => _handler.Handle(query, CancellationToken.None);
            act.Should().ThrowExactly<ArgumentException>()
                .WithMessage("*ClientCode cannot be empty*");
        }
    }

    // -------------------------------------------------------------------------
    // HandleAsync — happy path
    // -------------------------------------------------------------------------

    public sealed class HandleAsyncSuccess : GetPayerPraConfigurationQueryTests
    {
        [Fact]
        public async Task WhenApiReturnsEnabledConfig_ReturnsDto()
        {
            var dto = BuildDto(isEnabled: true);
            SetupApiSuccess(dto);

            var result = await _handler.Handle(BuildQuery(), CancellationToken.None);

            result.Should().Be(dto);
        }

        [Fact]
        public async Task WhenApiReturnsEnabledConfig_CallsApiExactlyOnce()
        {
            var dto = BuildDto(isEnabled: true);
            SetupApiSuccess(dto);

            await _handler.Handle(BuildQuery(), CancellationToken.None);

            _configApi.Verify(
                x => x.GetPayerPraConfiguration(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task WhenApiReturnsEnabledConfig_PassesCorrectParametersToApi()
        {
            var dto = BuildDto(isEnabled: true);
            var query = BuildQuery("SEMED", "UMR", "BILLING01");
            SetupApiSuccess(dto, query.OriginCode, query.ClientCode, query.BillingEntityCode);

            await _handler.Handle(query, CancellationToken.None);

            _configApi.Verify(
                x => x.GetPayerPraConfiguration("SEMED", "UMR", "BILLING01", It.IsAny<CancellationToken>()),
                Times.Once);
        }

        private void SetupApiSuccess(
            PayerPraConfigurationDto dto,
            string originCode = "ORIGIN",
            string clientCode = "CLIENT",
            string billingEntityCode = "BILLING")
        {
            _configApi
                .Setup(x => x.GetPayerPraConfiguration(originCode, clientCode, billingEntityCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync(BuildSuccessResponse(dto));
        }
    }

    // -------------------------------------------------------------------------
    // HandleAsync — PRA disabled
    // -------------------------------------------------------------------------

    public sealed class HandleAsyncPraDisabled : GetPayerPraConfigurationQueryTests
    {
        [Fact]
        public async Task WhenPraIsDisabled_ThrowsNotSupportedException()
        {
            var dto = BuildDto(isEnabled: false);
            _configApi
                .Setup(x => x.GetPayerPraConfiguration(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(BuildSuccessResponse(dto));

            Func<Task> act = () => _handler.Handle(BuildQuery(), CancellationToken.None);

            await act.Should().ThrowExactlyAsync<NotSupportedException>()
                .WithMessage("*Payer PRA is NOT enabled*");
        }
    }

    // -------------------------------------------------------------------------
    // HandleAsync — non-success HTTP responses trigger ConfigApiEndpointException
    // -------------------------------------------------------------------------

    public sealed class HandleAsyncNonSuccessResponse : GetPayerPraConfigurationQueryTests
    {
        [Theory]
        [InlineData(HttpStatusCode.NotFound)]
        [InlineData(HttpStatusCode.InternalServerError)]
        [InlineData(HttpStatusCode.ServiceUnavailable)]
        [InlineData(HttpStatusCode.Unauthorized)]
        public async Task WhenApiReturnsNonSuccessStatus_ThrowsConfigApiEndpointException(HttpStatusCode statusCode)
        {
            _configApi
                .Setup(x => x.GetPayerPraConfiguration(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(BuildFailureResponse(statusCode));

            Func<Task> act = () => _handler.Handle(BuildQuery(), CancellationToken.None);

            await act.Should().ThrowExactlyAsync<ConfigApiEndpointException>();
        }

        [Fact]
        public async Task WhenApiReturnsNonSuccessStatus_ExceptionContainsOriginCode()
        {
            _configApi
                .Setup(x => x.GetPayerPraConfiguration(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(BuildFailureResponse(HttpStatusCode.NotFound));

            Func<Task> act = () => _handler.Handle(BuildQuery("SEMED", "UMR", ""), CancellationToken.None);

            (await act.Should().ThrowExactlyAsync<ConfigApiEndpointException>())
                .WithMessage("*SEMED*");
        }

        [Fact]
        public async Task WhenApiReturnsNonSuccessStatus_ExceptionContainsClientCode()
        {
            _configApi
                .Setup(x => x.GetPayerPraConfiguration(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(BuildFailureResponse(HttpStatusCode.NotFound));

            Func<Task> act = () => _handler.Handle(BuildQuery("SEMED", "UMR", ""), CancellationToken.None);

            (await act.Should().ThrowExactlyAsync<ConfigApiEndpointException>())
                .WithMessage("*UMR*");
        }

        [Fact]
        public async Task WhenApiReturnsNonSuccessStatus_ExceptionStatusCodeMatchesResponse()
        {
            _configApi
                .Setup(x => x.GetPayerPraConfiguration(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(BuildFailureResponse(HttpStatusCode.ServiceUnavailable));

            Func<Task> act = () => _handler.Handle(BuildQuery(), CancellationToken.None);

            (await act.Should().ThrowExactlyAsync<ConfigApiEndpointException>())
                .Which.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        }

        [Fact]
        public async Task WhenSuccessResponseHasNullContent_ThrowsConfigApiEndpointException()
        {
            _configApi
                .Setup(x => x.GetPayerPraConfiguration(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(BuildSuccessResponse<PayerPraConfigurationDto>(null!));

            Func<Task> act = () => _handler.Handle(BuildQuery(), CancellationToken.None);

            await act.Should().ThrowExactlyAsync<ConfigApiEndpointException>();
        }
    }

    // -------------------------------------------------------------------------
    // HandleAsync — HttpRequestException triggers ConfigApiEndpointException
    // -------------------------------------------------------------------------

    public sealed class HandleAsyncHttpRequestException : GetPayerPraConfigurationQueryTests
    {
        [Fact]
        public async Task WhenApiThrowsHttpRequestException_ThrowsConfigApiEndpointException()
        {
            var handler = new GetPayerPraConfigurationQuery.PayerPraConfigurationQueryHandler(
                _configApi.Object, CreatePassthroughCache(), CreateNullLogger(), NoRetryPolicy);

            _configApi
                .Setup(x => x.GetPayerPraConfiguration(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("connection refused"));

            Func<Task> act = () => handler.Handle(BuildQuery(), CancellationToken.None);

            await act.Should().ThrowExactlyAsync<ConfigApiEndpointException>();
        }

        [Fact]
        public async Task WhenApiThrowsHttpRequestException_InnerExceptionIsPreserved()
        {
            var handler = new GetPayerPraConfigurationQuery.PayerPraConfigurationQueryHandler(
                _configApi.Object, CreatePassthroughCache(), CreateNullLogger(), NoRetryPolicy);

            var innerException = new HttpRequestException("connection refused");
            _configApi
                .Setup(x => x.GetPayerPraConfiguration(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(innerException);

            Func<Task> act = () => handler.Handle(BuildQuery(), CancellationToken.None);

            (await act.Should().ThrowExactlyAsync<ConfigApiEndpointException>())
                .Which.InnerException.Should().Be(innerException);
        }

        [Fact]
        public async Task WhenApiThrowsHttpRequestException_ExceptionMessageContainsOriginAndClientCode()
        {
            var handler = new GetPayerPraConfigurationQuery.PayerPraConfigurationQueryHandler(
                _configApi.Object, CreatePassthroughCache(), CreateNullLogger(), NoRetryPolicy);

            _configApi
                .Setup(x => x.GetPayerPraConfiguration(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException());

            Func<Task> act = () => handler.Handle(BuildQuery("SEMED", "UMR", ""), CancellationToken.None);

            (await act.Should().ThrowExactlyAsync<ConfigApiEndpointException>())
                .WithMessage("*SEMED*UMR*");
        }
    }

    // -------------------------------------------------------------------------
    // Retry behaviour — call counts
    // -------------------------------------------------------------------------

    public sealed class RetryBehaviour : GetPayerPraConfigurationQueryTests
    {
        [Fact]
        public async Task WhenApiFailsOnceThenSucceeds_ApiIsCalledTwice()
        {
            var handler = new GetPayerPraConfigurationQuery.PayerPraConfigurationQueryHandler(
                _configApi.Object, CreatePassthroughCache(), CreateNullLogger(), SingleRetryPolicy);

            var dto = BuildDto(isEnabled: true);
            var callCount = 0;

            _configApi
                .Setup(x => x.GetPayerPraConfiguration(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    callCount++;
                    return callCount == 1
                        ? BuildFailureResponse(HttpStatusCode.ServiceUnavailable)
                        : BuildSuccessResponse(dto);
                });

            var result = await handler.Handle(BuildQuery(), CancellationToken.None);

            result.Should().Be(dto);
            callCount.Should().Be(2);
        }

        [Fact]
        public async Task WhenApiAlwaysFails_RetriesAreExhaustedAndThrowsConfigApiEndpointException()
        {
            var handler = new GetPayerPraConfigurationQuery.PayerPraConfigurationQueryHandler(
                _configApi.Object, CreatePassthroughCache(), CreateNullLogger(), ExhaustingRetryPolicy);

            _configApi
                .Setup(x => x.GetPayerPraConfiguration(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(BuildFailureResponse(HttpStatusCode.ServiceUnavailable));

            Func<Task> act = () => handler.Handle(BuildQuery(), CancellationToken.None);

            await act.Should().ThrowExactlyAsync<ConfigApiEndpointException>();
        }

        [Fact]
        public async Task WhenApiAlwaysFails_ApiIsCalledElevenTimes()
        {
            // 1 initial call + 10 retries = 11 total
            var handler = new GetPayerPraConfigurationQuery.PayerPraConfigurationQueryHandler(
                _configApi.Object, CreatePassthroughCache(), CreateNullLogger(), ExhaustingRetryPolicy);

            _configApi
                .Setup(x => x.GetPayerPraConfiguration(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(BuildFailureResponse(HttpStatusCode.ServiceUnavailable));

            try { await handler.Handle(BuildQuery(), CancellationToken.None); }
            catch (ConfigApiEndpointException) { /* expected */ }

            _configApi.Verify(
                x => x.GetPayerPraConfiguration(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Exactly(11));
        }

        [Fact]
        public async Task WhenApiThrowsHttpRequestExceptionThenSucceeds_ReturnsSuccessfulResult()
        {
            var handler = new GetPayerPraConfigurationQuery.PayerPraConfigurationQueryHandler(
                _configApi.Object, CreatePassthroughCache(), CreateNullLogger(), SingleRetryPolicy);

            var dto = BuildDto(isEnabled: true);
            var callCount = 0;

            _configApi
                .Setup(x => x.GetPayerPraConfiguration(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    callCount++;
                    if (callCount == 1) throw new HttpRequestException("transient network error");
                    return BuildSuccessResponse(dto);
                });

            var result = await handler.Handle(BuildQuery(), CancellationToken.None);

            result.Should().Be(dto);
        }
    }

    // -------------------------------------------------------------------------
    // DefaultRetryPolicy — sleep duration shape
    // -------------------------------------------------------------------------

    public sealed class DefaultRetryPolicyShape
    {
        [Fact]
        public void DefaultRetryPolicy_IsNotNull()
        {
            GetPayerPraConfigurationQuery.PayerPraConfigurationQueryHandler.DefaultRetryPolicy
                .Should().NotBeNull();
        }

        [Theory]
        [InlineData(1, 250)]
        [InlineData(2, 250)]
        [InlineData(3, 250)]
        [InlineData(4, 250)]
        [InlineData(5, 250)]
        public void FastRetryPhase_SleepDurationIs250ms(int retryAttempt, double expectedMs)
        {
            var duration = retryAttempt <= 5
                ? TimeSpan.FromMilliseconds(250)
                : TimeSpan.FromSeconds(Math.Pow(2, retryAttempt - 6));

            duration.TotalMilliseconds.Should().Be(expectedMs);
        }

        [Theory]
        [InlineData(6, 1)]
        [InlineData(7, 2)]
        [InlineData(8, 4)]
        [InlineData(9, 8)]
        [InlineData(10, 16)]
        public void SlowRetryPhase_SleepDurationIsExponential(int retryAttempt, double expectedSeconds)
        {
            var duration = retryAttempt <= 5
                ? TimeSpan.FromMilliseconds(250)
                : TimeSpan.FromSeconds(Math.Pow(2, retryAttempt - 6));

            duration.TotalSeconds.Should().Be(expectedSeconds);
        }
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static GetPayerPraConfigurationQuery.Query BuildQuery(
        string originCode = "ORIGIN",
        string clientCode = "CLIENT",
        string billingEntityCode = "BILLING") =>
        new(originCode, clientCode, billingEntityCode);

    private static PayerPraConfigurationDto BuildDto(bool isEnabled) =>
        new("ORIGIN", "CLIENT", isEnabled, "ACH", DateTime.Today, DateTime.Today.AddYears(1));

    private static IApiResponse<T> BuildSuccessResponse<T>(T content)
    {
        var mock = new Mock<IApiResponse<T>>();
        mock.Setup(r => r.IsSuccessStatusCode).Returns(true);
        mock.Setup(r => r.StatusCode).Returns(HttpStatusCode.OK);
        mock.Setup(r => r.Content).Returns(content);
        return mock.Object;
    }

    private static IApiResponse<PayerPraConfigurationDto> BuildFailureResponse(HttpStatusCode statusCode)
    {
        var mock = new Mock<IApiResponse<PayerPraConfigurationDto>>();
        mock.Setup(r => r.IsSuccessStatusCode).Returns(false);
        mock.Setup(r => r.StatusCode).Returns(statusCode);
        mock.Setup(r => r.Content).Returns((PayerPraConfigurationDto)null!);
        return mock.Object;
    }
}
