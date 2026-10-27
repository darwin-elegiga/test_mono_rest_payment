using FluentAssertions;
using Moq;
using VPay.Gateway.Common.Kubera.Models;
using Xunit;

namespace VPay.Gateway.Common.Kubera.Tests;

public class GetTransactionByIdQueryHandlerTests : IDisposable
{
    private readonly Mock<IKuberaApi> _mockKubera = new Mock<IKuberaApi>(MockBehavior.Strict);
    private readonly GetTransactionByIdQueryHandler _handler;

    public GetTransactionByIdQueryHandlerTests()
    {
        _handler = new GetTransactionByIdQueryHandler(_mockKubera.Object);
    }

    public void Dispose()
    {
        _mockKubera.VerifyAll();
    }

    public class Constructor : GetTransactionByIdQueryHandlerTests
    {
        [Fact]
        public void WhenDocumentsApiIsNull_ThrowsArgumentNullException() =>
            new Action(() => new GetTransactionByIdQueryHandler(null))
                .Should().ThrowExactly<ArgumentNullException>()
                .And.ParamName.Should().Be("api");
    }

    public class Handle : GetTransactionByIdQueryHandlerTests
    {
        [Fact]
        public async Task WhenApiReturnsNoResults_ThenShouldReturnNull()
        {
            long id = 100;

            _mockKubera
                .Setup(x => x.GetTransactionsByIds(It.Is<long[]>(x => x.Length == 1 && x[0] == id), CancellationToken.None))
                .ReturnsAsync(Array.Empty<Transaction>())
                .Verifiable();

            var query = new GetTransactionByIdQuery(id);
            var result = await _handler.Handle(query, CancellationToken.None).ConfigureAwait(false);
            result.Should().BeNull();
        }

        [Fact]
        public async Task WhenApiReturnsMultipleResults_ThenShouldReturnFirstObject()
        {
            long id = 100;

            var passValue = new Transaction[]
            {
                    new Transaction
                    {
                        TransactionId = 1,
                    },
                    new Transaction
                    {
                        TransactionId = -2,
                    },
            };

            _mockKubera
                .Setup(x => x.GetTransactionsByIds(It.Is<long[]>(x => x.Length == 1 && x[0] == id), CancellationToken.None))
                .ReturnsAsync(passValue)
                .Verifiable();

            var query = new GetTransactionByIdQuery(id);
            var result = await _handler.Handle(query, CancellationToken.None).ConfigureAwait(false);
            result.Should().Be(passValue[0]);
        }
    }
}
