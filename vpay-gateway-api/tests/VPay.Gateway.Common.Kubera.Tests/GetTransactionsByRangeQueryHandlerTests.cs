using FluentAssertions;
using Moq;
using VPay.Gateway.Common.Kubera.Models;
using Xunit;

namespace VPay.Gateway.Common.Kubera.Tests;

public class GetTransactionsByRangeQueryHandlerTests : IDisposable
{
    private readonly Mock<IKuberaApi> _mockKubera = new Mock<IKuberaApi>(MockBehavior.Strict);
    private readonly GetTransactionsByRangeQueryHandler _handler;

    public GetTransactionsByRangeQueryHandlerTests()
    {
        _handler = new GetTransactionsByRangeQueryHandler(_mockKubera.Object);
    }

    public void Dispose()
    {
        _mockKubera.VerifyAll();
    }

    public class Constructor : GetTransactionsByRangeQueryHandlerTests
    {
        [Fact]
        public void WhenDocumentsApiIsNull_ThrowsArgumentNullException() =>
            new Action(() => new GetTransactionsByRangeQueryHandler(null))
                .Should().ThrowExactly<ArgumentNullException>()
                .And.ParamName.Should().Be("api");
    }

    public class Handle : GetTransactionsByRangeQueryHandlerTests
    {
        [Fact]
        public async Task WhenApiReturnsNoResults_ThenShouldReturnEmptyList()
        {
            long startId = 100;
            long endId = 105;

            _mockKubera
                .Setup(x => x.GetTransactionsByRange(startId, endId, CancellationToken.None))
                .ReturnsAsync(Array.Empty<Transaction>())
                .Verifiable();

            var query = new GetTransactionsByRangeQuery(startId, endId);
            var result = await _handler.Handle(query, CancellationToken.None).ConfigureAwait(false);
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task WhenApiReturnsMultipleResults_ThenShouldReturnListOfTransactionsFound()
        {
            long startId = 99;
            long endId = 107;

            var passValue = new Transaction[]
            {
                    new Transaction
                    {
                        TransactionId = 100,
                    },
                    new Transaction
                    {
                        TransactionId = 101,
                    },
            };

            _mockKubera
                .Setup(x => x.GetTransactionsByRange(startId, endId, CancellationToken.None))
                .ReturnsAsync(passValue)
                .Verifiable();

            var query = new GetTransactionsByRangeQuery(startId, endId);
            var result = await _handler.Handle(query, CancellationToken.None).ConfigureAwait(false);
            result.Should().BeEquivalentTo(passValue);
        }
    }
}
