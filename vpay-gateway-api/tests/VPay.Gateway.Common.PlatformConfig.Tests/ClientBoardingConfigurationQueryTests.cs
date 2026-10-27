using System.Net;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentValidation;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Refit;
using VPay.Extensions.Caching.Memory;
using Xunit;

namespace VPay.Gateway.Common.PlatformConfig.Tests;

public class ClientBoardingConfigurationQueryTests : IDisposable
{
    private readonly ClientBoardingConfigurationQueryHandler _handler;
    private readonly Mock<IPlatformConfigApi> _configApiMock = new(MockBehavior.Strict);
    private readonly Mock<IMemoryCache<ClientConfigurationDto?>> _memoryCacheMock = new(MockBehavior.Strict);

    public ClientBoardingConfigurationQueryTests()
    {
        _handler = new ClientBoardingConfigurationQueryHandler(_configApiMock.Object, _memoryCacheMock.Object);
    }

    public void Dispose()
    {
        _configApiMock.VerifyAll();
        _memoryCacheMock.VerifyAll();
        GC.SuppressFinalize(this);
    }

    public class Constructor : ClientBoardingConfigurationQueryTests
    {
        [Fact]
        public void WhenPlatformConfigIsNull_ThrowsArgumentNullException()
        {
            new Action(() => _ = new ClientBoardingConfigurationQueryHandler(null!, _memoryCacheMock.Object))
                .Should().Throw<ArgumentNullException>()
                .And.ParamName.Should().Be("platformConfig");
        }

        [Fact]
        public void WhenMemoryCacheIsNull_ThrowsArgumentNullException()
        {
            new Action(() => _ = new ClientBoardingConfigurationQueryHandler(_configApiMock.Object, null!))
                .Should().Throw<ArgumentNullException>()
                .And.ParamName.Should().Be("cache");
        }
    }

    public class Handle : ClientBoardingConfigurationQueryTests
    {
        [Fact]
        public async Task WhenRequestIsNotFound_ReturnsNull()
        {
            string originCode = "SEMED";
            string clientCode = "ABC";
            string beCode = "ABC0000001";

            var apiException = ApiException.Create(new HttpRequestMessage(), HttpMethod.Get, new HttpResponseMessage(HttpStatusCode.NotFound), new RefitSettings()).Result;

            ClientBoardingConfigurationQuery request = new(originCode, clientCode, beCode);

            string cacheKey = $"CLIENT_BOARDING_CONFIG_KEY_{request.ConfigurationName}_{request.OriginCode}_{request.ClientCode}".ToUpperInvariant();

            _memoryCacheMock
                .Setup(x => x.GetOrCreateAsync(cacheKey, It.IsAny<Func<Task<ClientConfigurationDto?>>>(), It.IsAny<MemoryCacheEntryOptions>(), It.IsAny<bool>()))
                .ReturnsAsync((ClientConfigurationDto?)null);

            var result = await _handler.Handle(request, CancellationToken.None).ConfigureAwait(false);

            using (new AssertionScope())
            {
                result.Should().BeNull();
            }
        }

        [Fact]
        public async Task WhenRequestIsInvalid_ThrowsInvalidOperationException()
        {
            string configurationName = "PREFIX_USERKEY";
            string originCode = "SEMED";
            string clientCode = "ABC";
            string invalidOperationException = "An error happened while processing the request";

            var exception = new InvalidOperationException(invalidOperationException);

            ClientBoardingConfigurationQuery request = new(configurationName, originCode, clientCode);

            string cacheKey = $"CLIENT_BOARDING_CONFIG_KEY_{request.ConfigurationName}_{request.OriginCode}_{request.ClientCode}".ToUpperInvariant();

            _memoryCacheMock
                .Setup(x => x.GetOrCreateAsync(cacheKey, It.IsAny<Func<Task<ClientConfigurationDto?>>>(), It.IsAny<MemoryCacheEntryOptions>(), It.IsAny<bool>()))
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
        [InlineData("PREFIX_USERKEY_TOO_LONG")]
        public async Task WhenRequestHasInvalidConfigName_ThrowsValidationException(string configurationName)
        {
            string originCode = "SEMED";
            string clientCode = "ABC";

            ClientBoardingConfigurationQuery request = new(configurationName, originCode, clientCode);

            Func<Task> act = async () => await _handler.Handle(request, CancellationToken.None).ConfigureAwait(false);

            await act
                .Should().ThrowAsync<ValidationException>();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("CLIENT_CODE_TOO_LONG")]
        public async Task WhenRequestHasInvalidClientCode_ThrowsValidationException(string clientCode)
        {
            string configurationName = "PREFIX_USERKEY";
            string originCode = "SEMED";

            ClientBoardingConfigurationQuery request = new(configurationName, originCode, clientCode);

            Func<Task> act = async () => await _handler.Handle(request, CancellationToken.None).ConfigureAwait(false);

            await act
                .Should().ThrowAsync<ValidationException>();
        }

        [Theory]
        [InlineData("SEMED", "ABC", "INACTIVE", "N")]
        [InlineData("SEMED", "ABC", "INACTIVE", "Y")]
        [InlineData("SEMED", "ABC", "ACTIVE", "N")]
        [InlineData("SEMED", "ABC", "ACTIVE", "Y")]
        public async Task WhenRequestIsValid_ReturnsConfiguration(string originCode, string clientCode, string status, string enabled)
        {
            string configurationName = "PREFIX_USERKEY";

            ClientBoardingConfigurationQuery request = new(configurationName, originCode, clientCode);

            ClientConfigurationDto config = new()
            {
                OriginCode = originCode,
                ClientCode = clientCode,
                Status = status,
                Data1 = enabled,
            };

            string cacheKey = $"CLIENT_BOARDING_CONFIG_KEY_{request.ConfigurationName}_{request.OriginCode}_{request.ClientCode}".ToUpperInvariant();

            _memoryCacheMock
                .Setup(x => x.GetOrCreateAsync(cacheKey, It.IsAny<Func<Task<ClientConfigurationDto?>>>(), It.IsAny<MemoryCacheEntryOptions>(), It.IsAny<bool>()))
                .ReturnsAsync(config);

            var result = await _handler.Handle(request, CancellationToken.None).ConfigureAwait(false);

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result!.OriginCode.Should().Be(originCode);
                result!.ClientCode.Should().Be(clientCode);
                result!.Status.Should().Be(status);
                result!.Data1.Should().Be(enabled);
            }
        }
    }
}
