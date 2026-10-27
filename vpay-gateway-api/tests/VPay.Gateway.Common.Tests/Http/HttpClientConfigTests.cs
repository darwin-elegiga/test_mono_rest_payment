using FluentAssertions;
using VPay.Gateway.Common.Http;
using Xunit;

namespace VPay.Gateway.Common.Tests.Http;

/// <summary>
/// Tests for the <see cref="HttpClientConfig"/>.
/// </summary>
public class HttpClientConfigTests
{
    [Fact]
    public void Timeout_DefaultValue_ShouldBeThirtySeconds() =>
        new HttpClientConfig().Timeout.Should().Be(TimeSpan.FromSeconds(30));
}
