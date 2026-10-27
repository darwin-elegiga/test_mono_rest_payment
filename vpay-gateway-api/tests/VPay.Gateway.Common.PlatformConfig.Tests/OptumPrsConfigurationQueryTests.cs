using System.Net;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Refit;
using VPay.Extensions.Caching.Memory;
using Xunit;
using FluentValidation;

namespace VPay.Gateway.Common.PlatformConfig.Tests;

public class OptumPrsConfigurationQueryTests : IDisposable
{
    private readonly OptumPrsConfigurationQueryHandler _handler;
    private readonly Mock<IPlatformConfigApi> _configApiMock = new(MockBehavior.Strict);
    private readonly Mock<IMemoryCache<OptumPrsConfigurationDto?>> _memoryCacheMock = new(MockBehavior.Strict);

    public OptumPrsConfigurationQueryTests()
    {
        _handler = new OptumPrsConfigurationQueryHandler(_configApiMock.Object, _memoryCacheMock.Object);
    }

    public void Dispose()
    {
        _configApiMock.VerifyAll();
        _memoryCacheMock.VerifyAll();
        GC.SuppressFinalize(this);
    }

    public class Constructor : OptumPrsConfigurationQueryTests
    {
        [Fact]
        public void WhenConfigApiIsNull_ThrowsArgumentNullException()
        {
            new Action(() => _ = new OptumPrsConfigurationQueryHandler(null!, _memoryCacheMock.Object))
                .Should().Throw<ArgumentNullException>()
                .And.ParamName.Should().Be("platformConfigApi");
        }

        [Fact]
        public void WhenMemoryCacheIsNull_ThrowsArgumentNullException()
        {
            new Action(() => _ = new OptumPrsConfigurationQueryHandler(_configApiMock.Object, null!))
                .Should().Throw<ArgumentNullException>()
                .And.ParamName.Should().Be("cache");
        }
    }

    public class Handle : OptumPrsConfigurationQueryTests
    {
        [Fact]
        public async Task WhenRequestIsNotFound_ReturnsNull()
        {
            string originCode = "SEMED";
            string clientCode = "ABC";
            string beCode = "ABC0000001";

            var apiException = ApiException.Create(new HttpRequestMessage(), HttpMethod.Get, new HttpResponseMessage(HttpStatusCode.NotFound), new RefitSettings()).Result;

            OptumPrsConfigurationQuery request = new(originCode, clientCode, beCode);

            var cacheKey = $"OPTUM_PRS_CONFIG_KEY_{clientCode}_{beCode}".ToUpper();

            _memoryCacheMock
                .Setup(x => x.GetOrCreateAsync(cacheKey, It.IsAny<Func<Task<OptumPrsConfigurationDto?>>>(), It.IsAny<MemoryCacheEntryOptions>(), It.IsAny<bool>()))
                .ReturnsAsync((OptumPrsConfigurationDto?)null);

            var result = await _handler.Handle(request, CancellationToken.None).ConfigureAwait(false);

            using (new AssertionScope())
            {
                result.Should().BeNull();
            }
        }

        [Fact]
        public async Task WhenRequestIsInvalid_ThrowsException()
        {
            string originCode = "SEMED";
            string clientCode = "ABC";
            string beCode = "ABC0000001";
            string invalidOperationException = "An error happened while processing the request";

            var exception = new InvalidOperationException(invalidOperationException);

            OptumPrsConfigurationQuery request = new(originCode, clientCode, beCode);

            var cacheKey = $"OPTUM_PRS_CONFIG_KEY_{clientCode}_{beCode}".ToUpper();

            _memoryCacheMock
                .Setup(x => x.GetOrCreateAsync(cacheKey, It.IsAny<Func<Task<OptumPrsConfigurationDto?>>>(), It.IsAny<MemoryCacheEntryOptions>(), It.IsAny<bool>()))
                .ThrowsAsync(exception);

            Func<Task> act = async () => await _handler.Handle(request, CancellationToken.None).ConfigureAwait(false);

            await act
                .Should().ThrowAsync<InvalidOperationException>()
                .WithMessage(invalidOperationException);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("CLIENT_CODE_TOO_LONG")]
        public async Task WhenRequestHasInvalidClientCode_ThrowsValidationException(string clientCode)
        {
            string originCode = string.Empty;
            string beCode = "ABC0000001";

            OptumPrsConfigurationQuery request = new(originCode, clientCode, beCode);

            Func<Task> act = async () => await _handler.Handle(request, CancellationToken.None).ConfigureAwait(false);

            await act
                .Should().ThrowAsync<ValidationException>();
        }

        [Fact]
        public async Task WhenRequestIsValid_ReturnsPrsConfiguration()
        {
            string originCode = "SEMED";
            string clientCode = "ABC";
            string beCode = "ABC0000001";

            OptumPrsConfigurationQuery request = new(originCode, clientCode, beCode);

            OptumPrsConfigurationDto config = new()
            {
                OriginCode = originCode,
                ClientCode = clientCode,
                BillingEntityCode = beCode,
                PrfClientGroup = "VPAY5",
                IsPrsEnabled = false,
            };

            var cacheKey = $"OPTUM_PRS_CONFIG_KEY_{clientCode}_{beCode}".ToUpper();

            _memoryCacheMock
                .Setup(x => x.GetOrCreateAsync(cacheKey, It.IsAny<Func<Task<OptumPrsConfigurationDto?>>>(), It.IsAny<MemoryCacheEntryOptions>(), It.IsAny<bool>()))
                .ReturnsAsync(config);

            var result = await _handler.Handle(request, CancellationToken.None).ConfigureAwait(false);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result!.OriginCode.Should().Be(originCode);
                result!.ClientCode.Should().Be(clientCode);
                result!.BillingEntityCode.Should().Be(beCode);
            }
        }
    }
}
