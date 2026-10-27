using FluentValidation.TestHelper;
using VPay.Gateway.Common.Preferencing.Models;

namespace VPay.Gateway.Common.Preferencing.Tests.Models;

/// <summary>
/// Tests for <see cref="NetworkEnrollmentHeaderDto"/>
/// </summary>
public class NetworkEnrollmentHeaderDtoValidatorTests
{
    private readonly NetworkEnrollmentHeaderDtoValidator _validator = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("    ")]
    public void NetworkPartner_WhenEmpty_ShouldHaveError(string value)
    {
        var item = new NetworkEnrollmentHeaderDto
        {
            NetworkPartner = value
        };
        var result = _validator.TestValidate(item);
        result.ShouldHaveValidationErrorFor(x => x.NetworkPartner);
    }

    [Fact]
    public void NetworkPartner_WhenTooLong_ShouldHaveError()
    {
        var item = new NetworkEnrollmentHeaderDto
        {
            NetworkPartner = new string('A', 13)
        };
        var result = _validator.TestValidate(item);
        result.ShouldHaveValidationErrorFor(x => x.NetworkPartner);
    }

    [Fact]
    public void NetworkPartner_WhenValid_ShouldNotHaveError()
    {
        var item = new NetworkEnrollmentHeaderDto
        {
            NetworkPartner = "VALID-NAME"
        };
        var result = _validator.TestValidate(item);
        result.ShouldNotHaveValidationErrorFor(x => x.NetworkPartner);
    }

    [Fact]
    public void CorrelationId_WhenEmpty_ShouldHaveError()
    {
        var item = new NetworkEnrollmentHeaderDto();
        var result = _validator.TestValidate(item);
        result.ShouldHaveValidationErrorFor(x => x.CorrelationId);
    }

    [Fact]
    public void CorrelationId_WhenTooLong_ShouldHaveError()
    {
        var item = new NetworkEnrollmentHeaderDto
        {
            CorrelationId = new string('1', 37)
        };
        var result = _validator.TestValidate(item);
        result.ShouldHaveValidationErrorFor(x => x.CorrelationId);
    }

    [Fact]
    public void CorrelationId_WhenValid_ShouldNotHaveError()
    {
        var item = new NetworkEnrollmentHeaderDto
        {
            CorrelationId = "123456789-123456789-123456789-123456"
        };
        var result = _validator.TestValidate(item);
        result.ShouldNotHaveValidationErrorFor(x => x.CorrelationId);
    }

    [Theory]
    [InlineData(-1)]
    public void Count_WhenNegative_ShouldHaveError(int value)
    {
        var item = new NetworkEnrollmentHeaderDto
        {
            Count = value
        };
        var result = _validator.TestValidate(item);
        result.ShouldHaveValidationErrorFor(x => x.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(int.MaxValue)]
    public void Count_WhenValid_ShouldNotHaveError(int value)
    {
        var item = new NetworkEnrollmentHeaderDto
        {
            Count = value
        };
        var result = _validator.TestValidate(item);
        result.ShouldNotHaveValidationErrorFor(x => x.Count);
    }
}
