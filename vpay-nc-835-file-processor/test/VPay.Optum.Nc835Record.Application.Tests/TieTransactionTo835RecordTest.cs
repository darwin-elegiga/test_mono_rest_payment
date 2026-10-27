using System.Data;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using VPay.Optum.Data.Connection;
using VPay.Optum.Data.Sql.PrfFiles;
using VPay.Optum.Nc835Record.Application;
using Xunit;

namespace VPay.Optum.Nc835Record.Application.Tests;
public class TieTransactionTo835RecordTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<IDbUnitOfWork> _uowMock;
    private readonly Mock<ILogger<TieTransactionTo835Record.Handler>> _loggerMock;
    private readonly TieTransactionTo835Record.Handler _handler;

    public TieTransactionTo835RecordTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _uowMock = new Mock<IDbUnitOfWork>();
        _loggerMock = new Mock<ILogger<TieTransactionTo835Record.Handler>>();
        _handler = new TieTransactionTo835Record.Handler(_mediatorMock.Object, _uowMock.Object, _loggerMock.Object);
    }

   

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenTransactionIsNotFound()
    {
        // Arrange
        var command = new TieTransactionTo835Record.Command(1, 12345);

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetPrfTransactionByTransactionId.Query>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PrfTransaction?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }
}
