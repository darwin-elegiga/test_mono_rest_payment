using System.Runtime.Serialization;
using FluentAssertions;
using Xunit;

namespace VPay.Gateway.Common.Tests;

/// <summary>
/// Tests for <see cref="EnumHelpers"/>
/// </summary>
public class EnumHelperTests
{
   public enum SomeEnum
    {
        [EnumMember(Value = "")]
        None,
        [EnumMember(Value = "1st")]
        First,
        [EnumMember(Value = "2nd")]
        Second
    }

    [Theory]
    [InlineData(SomeEnum.None, "")]
    [InlineData(SomeEnum.First, "1st")]
    [InlineData(SomeEnum.Second, "2nd")]
    public void WhenEnumMemberIsRequested_ShouldReturnMember(SomeEnum value, string expectedMember)
    {
        var actualMember = value.ToMember();
        actualMember.Should().Be(expectedMember);
    }

    [Theory]
    [InlineData(SomeEnum.None, "")]
    [InlineData(SomeEnum.First, "1st")]
    [InlineData(SomeEnum.Second, "2nd")]
    public void WhenEnumValueIsRequested_ShouldReturnValue(SomeEnum expectedValue, string member)
    {
        var actualValue = member.ToEnum(SomeEnum.None);
        actualValue.Should().Be(expectedValue);
    }
}
