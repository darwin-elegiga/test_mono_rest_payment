using FluentAssertions;
using Moq;
using VPay.Gateway.Common.Kubera.Models;
using Xunit;

namespace VPay.Gateway.Common.Kubera.Tests;

public class GetTransactionsByIdListQueryHandlerTests : IDisposable
{
    private readonly Mock<IKuberaApi> _mockKubera = new Mock<IKuberaApi>(MockBehavior.Strict);
    private readonly GetTransactionsByIdListQueryHandler _handler;

    public GetTransactionsByIdListQueryHandlerTests()
    {
        _handler = new GetTransactionsByIdListQueryHandler(_mockKubera.Object);
    }

    public void Dispose()
    {
        _mockKubera.VerifyAll();
    }

    public class Constructor : GetTransactionsByIdListQueryHandlerTests
    {
        [Fact]
        public void WhenDocumentsApiIsNull_ThrowsArgumentNullException() =>
            new Action(() => new GetTransactionsByIdListQueryHandler(null))
                .Should().ThrowExactly<ArgumentNullException>()
                .And.ParamName.Should().Be("api");
    }

    public class Handle : GetTransactionsByIdListQueryHandlerTests
    {
        [Fact]
        public async Task WhenApiReturnsNoResults_ThenShouldReturnEmptyList()
        {
            long[] ids = new long[] { 100, 101 };

            _mockKubera
                .Setup(x => x.GetTransactionsByIds(It.Is<long[]>(x => x.SequenceEqual(ids)), CancellationToken.None))
                .ReturnsAsync(Array.Empty<Transaction>())
                .Verifiable();

            var query = new GetTransactionsByIdListQuery(ids.ToList());
            var result = await _handler.Handle(query, CancellationToken.None).ConfigureAwait(false);
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task WhenApiReturnsMultipleResults_ThenShouldReturnListOfTransactionsFound()
        {
            long[] ids = new long[] { 100, 101 };

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
                .Setup(x => x.GetTransactionsByIds(It.Is<long[]>(x => x.SequenceEqual(ids)), CancellationToken.None))
                .ReturnsAsync(passValue)
                .Verifiable();

            var query = new GetTransactionsByIdListQuery(ids.ToList());
            var result = await _handler.Handle(query, CancellationToken.None).ConfigureAwait(false);
            result.Should().BeEquivalentTo(passValue);
        }
    }
}
