using FluentAssertions;
using NSubstitute;
using VPay.Gateway.Common.Preferencing;
using VPay.Gateway.Common.Preferencing.Models;
using Xunit;

#nullable enable

namespace VPay.Gateway.Application.Tests;

/// <summary>
/// Tests for <see cref="BatchSolve"/>
/// </summary>
public class BatchSolveTests
{
    public readonly static IEnumerable<object?[]> HappyPathExpectations = new List<object?[]>
    {
        new object?[]
        {
            new List<BatchSolveRequest>
            {
                new BatchSolveRequest
                {
                    CorrelationId = "12345"
                }
            },
            new List<BatchSolveResponse>
            {
                new BatchSolveResponse
                {
                    CorrelationId = "12345",
                    PaymentTypeCode = "NOTREAL"
                }
            }
        },
    };

    private static readonly IPreferencingApi _preferencingApiMock = Substitute.For<IPreferencingApi>();
    private static readonly BatchSolve.Handler _handler = new BatchSolve.Handler(_preferencingApiMock);

    [Fact]
    public void WhenPreferencingApiIsNullThrowsArgumentNullException() =>
        new Action(() => _ = new BatchSolve.Handler(null!))
            .Should().ThrowExactly<ArgumentNullException>()
            .And.ParamName.Should().Be("preferencingApi");

    [Theory]
    [MemberData(nameof(HappyPathExpectations))]
    public async Task WhenValidParametersNoExceptionThrown(IEnumerable<BatchSolveRequest> request, IEnumerable<BatchSolveResponse> expected)
    {
        _preferencingApiMock
            .BatchSolve(Arg.Is(request), default)
            .Returns(Task.FromResult(expected));

        var query = new BatchSolve.Query(request);

        var actual = await _handler.Handle(query, default);

        actual.Should().BeEquivalentTo(expected);
    }
}
