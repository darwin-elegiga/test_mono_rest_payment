using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using VPay.Gateway.Identity.Events;
using Xunit;

namespace VPay.Gateway.Identity.Tests.Events;

/// <summary>
/// Tests for the <see cref="ClientCredentialsValidateCredentialsContext"/>.
/// </summary>
public class ClientCredentialsValidateCredentialsContextTests
{
    private readonly HttpContext _httpContext = new DefaultHttpContext();
    private readonly AuthenticationScheme _authenticationScheme = new AuthenticationScheme("Basic", "Basic", typeof(ClientCredentialsAuthenticationHandler));
    private readonly ClientCredentialsAuthenticationOptions _options = new ClientCredentialsAuthenticationOptions();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("    ")]
    public void Constructor_WithEmptyClientId_ThrowsArgumentException(string clientId) =>
        new Action(() => new ClientCredentialsValidateCredentialsContext(clientId, "password", _httpContext, _authenticationScheme, _options))
            .Should().ThrowExactly<ArgumentException>()
            .And.ParamName.Should().Be("clientId");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("    ")]
    public void Constructor_WithEmptyClientSecret_ThrowsArgumentException(string clientSecret) =>
        new Action(() => new ClientCredentialsValidateCredentialsContext("username", clientSecret, _httpContext, _authenticationScheme, _options))
            .Should().ThrowExactly<ArgumentException>()
            .And.ParamName.Should().Be("clientSecret");

    [Fact]
    public void ClientId_ShouldBeValueProvidedToConstructor()
    {
        const string Username = "TestUser";
        var context = new ClientCredentialsValidateCredentialsContext(Username, "TestPassword", _httpContext, _authenticationScheme, _options);
        context.ClientId.Should().Be(Username);
    }

    [Fact]
    public void ClientSecret_ShouldBeValueProvidedToConstructor()
    {
        const string Password = "TestPassword";
        var context = new ClientCredentialsValidateCredentialsContext("TestUser", Password, _httpContext, _authenticationScheme, _options);
        context.ClientSecret.Should().Be(Password);
    }
}
