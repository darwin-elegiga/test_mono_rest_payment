using System.Net;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Refit;
using VPay.Extensions.Caching.Memory;
using Xunit;

namespace VPay.Gateway.Common.PlatformConfig.Tests;

/// <summary>
/// Tests for the <see cref="ExternalClientConfigurationQueryHandler"/>
/// </summary>
public class ExternalClientConfigurationQueryHandlerTests : IDisposable
{
    private readonly Mock<IPlatformConfigApi> _mockApi = new Mock<IPlatformConfigApi>(MockBehavior.Strict);
    private readonly IMemoryCache<ExternalClientConfigurationDto?> _memoryCache;
    private readonly ExternalClientConfigurationQueryHandler _handler;

    public ExternalClientConfigurationQueryHandlerTests()
    {
        _memoryCache = new MemoryCache<ExternalClientConfigurationDto?>(new MemoryCache(new MemoryCacheOptions()));
        _handler = new ExternalClientConfigurationQueryHandler(_mockApi.Object, _memoryCache);
    }

    public void Dispose() =>
        _mockApi.VerifyAll();

    public class Constructor : ExternalClientConfigurationQueryHandlerTests
    {
        [Fact]
        public void WhenApiIsNull_ThrowsArgumentNullException() =>
            new Action(() => new ExternalClientConfigurationQueryHandler(null!, _memoryCache))
                .Should().ThrowExactly<ArgumentNullException>()
                .And.ParamName.Should().Be("platformConfigApi");

        [Fact]
        public void WhenCacheIsNull_ThrowsArgumentNullException() =>
            new Action(() => new ExternalClientConfigurationQueryHandler(_mockApi.Object, null!))
                .Should().ThrowExactly<ArgumentNullException>()
                .And.ParamName.Should().Be("cache");
    }

    public class Handle : ExternalClientConfigurationQueryHandlerTests
    {
        private const string ClientCode = "CLIENT";
        private readonly DateTime _date = DateTime.Now;

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("    ")]
        public async Task WhenClientCodeIsNotProvided_ShouldReturnNull(string clientCode)
        {
            var query = new ExternalClientConfigurationQuery(clientCode, _date);
            var result = await _handler.Handle(query, CancellationToken.None).ConfigureAwait(false);
            result.Should().BeNull();
        }

        [Fact]
        public async Task WhenConfigurationHasValue_ValueIsCached()
        {
            var expected = new ExternalClientConfigurationDto { ClientCode = ClientCode };

            _mockApi
                .SetupSequence(x => x.GetExternalClientConfiguration(ClientCode, _date, ExternalClientConfigurationType.Guidewire, CancellationToken.None))
                .ReturnsAsync(new[] { expected })
                .ReturnsAsync(new[] { new ExternalClientConfigurationDto { ClientCode = "OTHER_CLIENT" } });

            // run the query twice.  The first hit should add the result to the cache.  The second hit should retrieve from the cache
            var query = new ExternalClientConfigurationQuery(ClientCode, _date);
            var firstResult = await _handler.Handle(query, CancellationToken.None).ConfigureAwait(false);
            var secondResult = await _handler.Handle(query, CancellationToken.None).ConfigureAwait(false);

            using (new AssertionScope())
            {
                firstResult.Should().BeSameAs(expected);
                secondResult.Should().BeSameAs(expected);
            }
        }

        [Fact]
        public async Task WhenConfigurationCached_DifferentClientShouldNotReturnCachedItem()
        {
            var expected = new ExternalClientConfigurationDto { ClientCode = ClientCode };
            var secondExpected = new ExternalClientConfigurationDto { ClientCode = "OTHER_CLIENT" };

            _mockApi
                .SetupSequence(x => x.GetExternalClientConfiguration(It.IsAny<string>(), _date, ExternalClientConfigurationType.Guidewire, CancellationToken.None))
                .ReturnsAsync(new[] { expected })
                .ReturnsAsync(new[] { secondExpected });

            // run the query twice with different clients.  The second run should *not* return the configuration cached by the first hit
            var firstResult = await _handler.Handle(new ExternalClientConfigurationQuery(ClientCode, _date), CancellationToken.None).ConfigureAwait(false);
            var secondResult = await _handler.Handle(new ExternalClientConfigurationQuery("OTHER_CLIENT", _date), CancellationToken.None).ConfigureAwait(false);

            using (new AssertionScope())
            {
                firstResult.Should().BeSameAs(expected);
                secondResult.Should().BeSameAs(secondExpected);
            }
        }

        [Fact]
        public async Task WhenConfigurationIsNull_ValueIsCached()
        {
            var exception = await ApiException.Create(
                new HttpRequestMessage(),
                HttpMethod.Get,
                new HttpResponseMessage(HttpStatusCode.NotFound),
                new RefitSettings())
                .ConfigureAwait(false);

            _mockApi
                .SetupSequence(x => x.GetExternalClientConfiguration(ClientCode, _date, ExternalClientConfigurationType.Guidewire, CancellationToken.None))
                .ThrowsAsync(exception)
                .ThrowsAsync(new Exception());

            // run the query twice.  The first hit should add a null value to the cache.
            // The second hit (if it hits the api) should throw an unhandled exception.  If the exception
            // is not thrown, the value has been retrieved from the cache
            var query = new ExternalClientConfigurationQuery(ClientCode, _date);
            var firstResult = await _handler.Handle(query, CancellationToken.None).ConfigureAwait(false);
            var secondResult = await _handler.Handle(query, CancellationToken.None).ConfigureAwait(false);

            using (new AssertionScope())
            {
                firstResult.Should().BeNull();
                secondResult.Should().BeNull();
            }
        }
    }
}
