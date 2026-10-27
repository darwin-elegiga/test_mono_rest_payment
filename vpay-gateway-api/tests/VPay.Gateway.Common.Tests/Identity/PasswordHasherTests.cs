using System.Globalization;
using System.Security.Cryptography;
using FluentAssertions;
using Microsoft.Extensions.Options;
using VPay.Gateway.Common.Identity;
using Xunit;

namespace VPay.Gateway.Common.Tests.Identity;

public class PasswordHasherTests
{
    private readonly PasswordHasher _sut;

    private readonly HashingOptions _hashingOptions;

    private static readonly RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider();

    public PasswordHasherTests()
    {
        _hashingOptions = new HashingOptions { Iterations = 1000 };
        _sut = new PasswordHasher(Options.Create(_hashingOptions));
    }

    [Fact]
    public void Hash_WhenTheHashIsCompleted_ThenTheGenerateHashWillHaveThreePartsSeparatedByPeriods()
    {
        var password = RandomString(10);
        var result = _sut.Hash(password);

        var parts = result.Split('.');

        parts.Length.Should().Be(3);
    }

    [Fact]
    public void Hash_WhenHashIsCompleted_ThenTheFirstPartShouldBeTheNumberOfIterations()
    {
        var password = RandomString(10);
        var result = _sut.Hash(password);

        var parts = result.Split('.');

        parts[0].Should().Be(_hashingOptions.Iterations.ToString());
    }

    [Fact]
    public void VerifyPassword_WhenPasswordIsCorrect_ThenShouldReturnTrue()
    {
        var password = RandomString(10);
        var hash = _sut.Hash(password);

        var result = _sut.VerifyPassword(hash, password);

        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_WhenPasswordIsIncorrect_ThenShouldReturnFalse()
    {
        var password = RandomString(10);
        var diff = RandomString(5);
        var hash = _sut.Hash(password + diff);

        var result = _sut.VerifyPassword(hash, password);

        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_WhenPassedInHashDoesNotHave3Parts_ThenAFormatExceptionShouldBeThrown()
    {
        var password = RandomString(10);
        var first = RandomString(5);
        var second = RandomString(5);

        new Action(() => _sut.VerifyPassword($"{first}.{second}", password))
            .Should().ThrowExactly<FormatException>();
    }

    private static string RandomString(int length)
    {
        string chars = AppContext.BaseDirectory + DateTime.Now.Second.ToString(CultureInfo.InvariantCulture);
        byte[] randomBytes = new byte[length];
        rngCsp.GetBytes(randomBytes);
        return new string(randomBytes.Select(b => chars[b % chars.Length]).ToArray());
    }

}
