using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using VPay.Extensions.Caching.Memory;
using VPay.Optum.Http.ConfigApi;
using VPay.Optum.Http.ConfigApi.Models;
using VPay.Optum.Nc835FileProcessor.Application.Services;
using VPay.Optum.Nc835Record.Application.Queries;
using Xunit;

namespace VPay.Optum.Nc835Record.Application.Tests.Queries
{
    public class GetSubPayerIdTests
    {
        private readonly Mock<IConfigApi> _mockConfigApi;
        private readonly Mock<IMemoryCache<BillingEntity?>> _mockCache;
        private readonly GetSubPayerId.Handler _handler;

        public GetSubPayerIdTests()
        {
            _mockConfigApi = new Mock<IConfigApi>();
            _mockCache = new Mock<IMemoryCache<BillingEntity?>>();
            _handler = new GetSubPayerId.Handler(_mockConfigApi.Object, _mockCache.Object);
        }

        [Fact]
        public async Task Handle_ReturnsBillingEntityId_WhenCacheHit()
        {
            // Arrange
            var query = new GetSubPayerId.Query("Client1", "Billing1");
            var cachedEntity = new BillingEntity { Id = 123 };
            _mockCache
                .Setup(c => c.GetOrCreateAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<BillingEntity?>>>(),
                    It.IsAny<MemoryCacheEntryOptions>(),
                    It.IsAny<bool>()))
                .ReturnsAsync(cachedEntity);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Equal(123, result);
        }

        [Fact]
        public async Task Handle_ReturnsNull_WhenCacheMissAndApiReturnsNull()
        {
            // Arrange
            var query = new GetSubPayerId.Query("Client1", "Billing1");
            _mockCache
                .Setup(c => c.GetOrCreateAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<BillingEntity?>>>(),
                    It.IsAny<MemoryCacheEntryOptions>(),
                    It.IsAny<bool>()))
                .ReturnsAsync((BillingEntity?)null);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task Handle_CallsApi_WhenCacheMiss()
        {
            // Arrange
            var query = new GetSubPayerId.Query("Client1", "Billing1");
            var apiEntity = new BillingEntity { Id = 456 };
            _mockConfigApi
                .Setup(api => api.GetBillingEntity("Client1", "Billing1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(apiEntity);

            _mockCache
                .Setup(c => c.GetOrCreateAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<BillingEntity?>>>(),
                    It.IsAny<MemoryCacheEntryOptions>(),
                    It.IsAny<bool>()))
                .Returns<string, Func<Task<BillingEntity?>>, MemoryCacheEntryOptions, bool>((key, factory, options, flag) => factory());

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Equal(456, result);
            _mockConfigApi.Verify(api => api.GetBillingEntity("Client1", "Billing1", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public void GetKey_ReturnsCorrectKey()
        {
            // Arrange
            var query = new GetSubPayerId.Query("Client1", "Billing1");

            // Act
            var method = typeof(GetSubPayerId.Handler).GetMethod("GetKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var key = method?.Invoke(null, new object[] { query });

            // Assert
            Assert.Equal("BE_CLIENT1_BILLING1", key);
        }
    }
}
