using VPay.Gateway.Api.Models;
using Xunit;

namespace VPay.Gateway.Api.Tests.Models;

/// <summary>
/// Tests for <see cref="GetPocDtoValidator"/>
/// </summary>
public class GetPocDtoValidatorTests
{
    [Fact]
    public void Validate_ValidDto_ReturnsSuccess()
    {
        // Arrange
        var validator = new GetPocDtoValidator();
        var dto = new GetPocDto
        {
            BillingEntityId = "12345",
            TaxId = "67890",
            ZipCode = "12345",
            Amount = 100.00m
        };
        // Act
        var result = validator.Validate(dto);
        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_InvalidDto_ReturnsErrors()
    {
        // Arrange
        var validator = new GetPocDtoValidator();
        var dto = new GetPocDto
        {
            BillingEntityId = "", // Invalid: empty
            TaxId = "12345678901", // Invalid: too long
            ZipCode = "ABCDE", // Invalid: not a valid zip code
            Amount = null // Invalid: null
        };
        // Act
        var result = validator.Validate(dto);
        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(4, result.Errors.Count);
    }
}
