using System.Net;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Refit;
using VPay.Extensions.Caching.Memory;
using Xunit;

namespace VPay.Gateway.Common.PlatformConfig.Tests;

public class OptumSubPayerQueryTests : IDisposable
{
    private readonly OptumSubPayerQueryHandler _handler;
    private readonly Mock<IPlatformConfigApi> _configApiMock = new(MockBehavior.Strict);
    private readonly Mock<IMemoryCache<OptumSubPayerDto?>> _memoryCacheMock = new(MockBehavior.Strict);

    public OptumSubPayerQueryTests()
    {
        _handler = new OptumSubPayerQueryHandler(_configApiMock.Object, _memoryCacheMock.Object);
    }

    public void Dispose()
    {
        _configApiMock.VerifyAll();
        _memoryCacheMock.VerifyAll();
        GC.SuppressFinalize(this);
    }

    public class Constructor : OptumSubPayerQueryTests
    {
        [Fact]
        public void WhenConfigApiIsNull_ThrowsArgumentNullException()
        {
            new Action(() => _ = new OptumSubPayerQueryHandler(null!, _memoryCacheMock.Object))
                .Should().Throw<ArgumentNullException>()
                .And.ParamName.Should().Be("platformConfigApi");
        }

        [Fact]
        public void WhenMemoryCacheIsNull_ThrowsArgumentNullException()
        {
            new Action(() => _ = new OptumSubPayerQueryHandler(_configApiMock.Object, null!))
                .Should().Throw<ArgumentNullException>()
                .And.ParamName.Should().Be("cache");
        }
    }

    public class Handle : OptumSubPayerQueryTests
    {
        [Fact]
        public async Task WhenRequestIsNotFound_ReturnsNull()
        {
            string subPayerId = "ABC00001";

            var apiException = ApiException.Create(new HttpRequestMessage(), HttpMethod.Get, new HttpResponseMessage(HttpStatusCode.NotFound), new RefitSettings()).Result;

            OptumSubPayerQuery request = new(subPayerId);

            var cacheKey = $"OPTUM_SUBPAY_KEY_{subPayerId}".ToUpper();

            _memoryCacheMock
                .Setup(x => x.GetOrCreateAsync(cacheKey, It.IsAny<Func<Task<OptumSubPayerDto?>>>(), It.IsAny<MemoryCacheEntryOptions>(), It.IsAny<bool>()))
                .ReturnsAsync((OptumSubPayerDto?)null);

            var result = await _handler.Handle(request, CancellationToken.None).ConfigureAwait(false);

            using (new AssertionScope())
            {
                result.Should().BeNull();
            }
        }

        [Fact]
        public async Task WhenRequestIsInvalid_ThrowsException()
        {
            string subPayerId = "ABC00001";
            string invalidOperationException = "An error happened while processing the request";

            var exception = new InvalidOperationException(invalidOperationException);

            OptumSubPayerQuery request = new(subPayerId);

            var cacheKey = $"OPTUM_SUBPAY_KEY_{subPayerId}".ToUpper();

            _memoryCacheMock
                .Setup(x => x.GetOrCreateAsync(cacheKey, It.IsAny<Func<Task<OptumSubPayerDto?>>>(), It.IsAny<MemoryCacheEntryOptions>(), It.IsAny<bool>()))
                .ThrowsAsync(exception);

            Func<Task> act = async () => await _handler.Handle(request, CancellationToken.None).ConfigureAwait(false);

            await act
                .Should().ThrowAsync<InvalidOperationException>()
                .WithMessage(invalidOperationException);
        }

        [Fact]
        public async Task WhenRequestIsValid_ReturnsPrsConfiguration()
        {
            string subPayerId = "ABC00001";

            OptumSubPayerQuery request = new(subPayerId);

            OptumSubPayerDto config = new()
            {
                OptumSubPayorId = subPayerId,
            };

            var cacheKey = $"OPTUM_SUBPAY_KEY_{subPayerId}".ToUpper();

            _memoryCacheMock
                .Setup(x => x.GetOrCreateAsync(cacheKey, It.IsAny<Func<Task<OptumSubPayerDto?>>>(), It.IsAny<MemoryCacheEntryOptions>(), It.IsAny<bool>()))
                .ReturnsAsync(config);

            var result = await _handler.Handle(request, CancellationToken.None).ConfigureAwait(false);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result!.OptumSubPayorId.Should().Be(subPayerId);
            }
        }
    }
}
