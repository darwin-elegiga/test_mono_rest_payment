using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Polly;
using Xunit;

namespace VPay.Gateway.Common.Tests;

/// <summary>
/// Tests for the <see cref="PollyExtensions"/>
/// </summary>
public class PollyExtensionsTests
{
    [Fact]
    public void TryGetLogger_WhenContextHasLogger_ShouldReturnTrueAndLogger()
    {
        var result = new Context("test", new Dictionary<string, object>
        {
            [PolicyContextItems.Logger] = new NullLogger<object>()
        }).TryGetLogger(out var logger);

        result.Should().BeTrue();
        logger.Should().NotBeNull();
        logger.Should().BeOfType<NullLogger<object>>();
    }

    [Fact]
    public void TryGetLogger_WhenContextDoesNotHaveLogger_ShouldReturnTrueAndLogger()
    {
        var result = new Context().TryGetLogger(out var logger);
        result.Should().BeFalse();
        logger.Should().BeNull();
    }
}
