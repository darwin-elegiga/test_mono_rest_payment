using FluentAssertions;
using MediatR;
using Moq;
using VPay.Gateway.Common.Preferencing;
using Xunit;

namespace VPay.Gateway.Application.Tests;

/// <summary>
/// Tests for <see cref="PrsDecisionCommandHandler"/>
/// </summary>
public class PrsDecisionCommandHandlerTests : IDisposable
{
    private readonly Mock<IPreferencingApi> _preferencingApiMock = new(MockBehavior.Strict);
    private readonly Mock<IMediator> _mediatorMock = new(MockBehavior.Strict);
    private readonly PrsDecisionCommandHandler _handler;

    public PrsDecisionCommandHandlerTests() =>
        _handler = new PrsDecisionCommandHandler(_preferencingApiMock.Object);

    public void Dispose()
    {
        _preferencingApiMock.VerifyAll();
        _mediatorMock.VerifyAll();
    }
}

public class Constructor : PrsDecisionCommandHandlerTests
{
    [Fact]
    public void WhenEnrollmentsApiIsNull_ThrowsArgumentNullException() =>
        new Action(() => new PrsDecisionCommandHandler(null!))
            .Should().ThrowExactly<ArgumentNullException>()
            .And.ParamName.Should().Be("preferencingApi");
}

public class Handle : PrsDecisionCommandHandlerTests
{
}
