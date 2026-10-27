using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using VPay.Extensions.Caching.Memory;
using VPay.Gateway.Common.Kubera.Models;
using Xunit;

namespace VPay.Gateway.Common.Kubera.Tests;

/// <summary>
/// Tests for the <see cref="GetTransactionsByPaymentIdQueryHandler"/> class.
/// </summary>
public class GetTransactionsByPaymentIdQueryHandlerTests
{
    [SuppressMessage("ReSharper", "ObjectCreationAsStatement")]
    public class Constructor
    {
#pragma warning disable CA1806

        [Fact]
        public void WhenDataAccessorIsNull_ThrowsArgumentNullException() =>
            new Action(() => new GetTransactionsByPaymentIdQueryHandler(null!, Mock.Of<IMemoryCache<Transaction[]>>()))
                .Should().ThrowExactly<ArgumentNullException>()
                .And.ParamName.Should().Be("api");

        [Fact]
        public void WhenLoggerIsNull_ThrowsArgumentNullException() =>
            new Action(() => new GetTransactionsByPaymentIdQueryHandler(Mock.Of<IKuberaApi>(), null!))
                .Should().ThrowExactly<ArgumentNullException>()
                .And.ParamName.Should().Be("transactionsCache");

#pragma warning restore CA1806
    }

    public sealed class Handle : GetTransactionsByPaymentIdQueryHandlerTests, IDisposable
    {
        private const string ClientCode = "UMR";
        private const string PaymentId = "UMR001";

        private readonly Mock<IKuberaApi> _mockApi = new(MockBehavior.Strict);
        private readonly GetTransactionsByPaymentIdQueryHandler _handler;

        public Handle()
        {
            var cache = new MemoryCache<Transaction[]>(new MemoryCache(new MemoryCacheOptions()));
            _handler = new GetTransactionsByPaymentIdQueryHandler(_mockApi.Object, cache);
        }

        public void Dispose()
        {
            _mockApi.VerifyAll();
        }

        [Fact]
        public async Task Handle_WhenCalledMultipleTimes_ReturnsCachedValue()
        {
            var expected = new Transaction[]
            {
                    new()
                    {
                        TransactionId = 123
                    }
            };

            _mockApi
                .Setup(x => x.GetTransactionsByPaymentId(ClientCode, PaymentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected)
                .Verifiable();

            // run the query twice.  The first hit should add the result to the cache.  The second hit should retrieve from the cache
            var query = new GetTransactionsByPaymentIdQuery(ClientCode, PaymentId);
            var firstResult = await _handler.Handle(query, CancellationToken.None).ConfigureAwait(false);
            var secondResult = await _handler.Handle(query, CancellationToken.None).ConfigureAwait(false);

            using (new AssertionScope())
            {
                firstResult.Should().BeSameAs(expected);
                secondResult.Should().BeSameAs(expected);

                _mockApi.Verify(x => x.GetTransactionsByPaymentId(ClientCode, PaymentId, It.IsAny<CancellationToken>()), Times.Once);
            }
        }

        [Fact]
        public async Task Handle_WhenCalledMultipleTimesWithDifferentClients_DoesNotReturnCachedValue()
        {
            var expected = new Transaction[]
            {
                    new()
                    {
                        TransactionId = 123
                    }
            };

            var secondExpected = new Transaction[]
            {
                    new()
                    {
                        TransactionId = 123
                    }
            };

            _mockApi
                .SetupSequence(x => x.GetTransactionsByPaymentId(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected)
                .ReturnsAsync(secondExpected);

            var firstResult = await _handler.Handle(new GetTransactionsByPaymentIdQuery(ClientCode, PaymentId), CancellationToken.None).ConfigureAwait(false);
            var secondResult = await _handler.Handle(new GetTransactionsByPaymentIdQuery(ClientCode + "A", PaymentId + "1"), CancellationToken.None).ConfigureAwait(false);

            using (new AssertionScope())
            {
                firstResult.Should().BeSameAs(expected);
                secondResult.Should().BeSameAs(secondExpected);

                _mockApi.Verify(x => x.GetTransactionsByPaymentId(ClientCode, PaymentId, It.IsAny<CancellationToken>()), Times.Once);
                _mockApi.Verify(x => x.GetTransactionsByPaymentId(ClientCode + "A", PaymentId + "1", It.IsAny<CancellationToken>()), Times.Once);
            }
        }
    }
}
