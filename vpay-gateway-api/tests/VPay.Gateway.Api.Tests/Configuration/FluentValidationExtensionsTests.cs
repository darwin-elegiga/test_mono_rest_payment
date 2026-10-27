using FluentValidation;
using FluentValidation.Results;
using VPay.Gateway.Api.Configuration;
using Xunit;

namespace VPay.Gateway.Api.Tests.Configuration;

public class FluentValidationExtensionsTests
{
    [Fact]
    public void AsProblemDetails_WithValidValidationResult_ReturnsNull()
    {
        // Arrange
        var validationResult = new ValidationResult();

        // Act
        var result = validationResult.AsProblemDetails();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void AsProblemDetails_WithInvalidValidationResult_ReturnsProblemDetailsWithErrors()
    {
        // Arrange
        var failures = new List<ValidationFailure>
        {
            new("Field1", "Error1"),
            new("Field2", "Error2"),
            new("Field1", "Error3")
        };
        var validationResult = new ValidationResult(failures);

        // Act
        var result = validationResult.AsProblemDetails();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Errors.Count);
        Assert.True(result.Errors.ContainsKey("Field1"));
        Assert.True(result.Errors.ContainsKey("Field2"));
        Assert.Equal(["Error1", "Error3"], result.Errors["Field1"]);
        Assert.Equal(["Error2"], result.Errors["Field2"]);
    }

    [Fact]
    public void AsProblemDetails_WithNullValidationResult_ReturnsNull()
    {
        // Act
        var result = ((ValidationResult)null).AsProblemDetails();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void AsProblemDetails_WithValidationExceptionWithNoErrors_ReturnsNull()
    {
        // Arrange
        var exception = new ValidationException([]);

        // Act
        var result = exception.AsProblemDetails();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void AsProblemDetails_WithValidationExceptionWithErrors_ReturnsProblemDetailsWithErrors()
    {
        // Arrange
        var failures = new List<ValidationFailure>
        {
            new("FieldA", "ErrorA"),
            new("FieldB", "ErrorB"),
            new("FieldA", "ErrorC")
        };
        var exception = new ValidationException(failures);

        // Act
        var result = exception.AsProblemDetails();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Errors.Count);
        Assert.True(result.Errors.ContainsKey("FieldA"));
        Assert.True(result.Errors.ContainsKey("FieldB"));
        Assert.Equal(["ErrorA", "ErrorC"], result.Errors["FieldA"]);
        Assert.Equal(["ErrorB"], result.Errors["FieldB"]);
    }

    [Fact]
    public void AsProblemDetails_WithNullValidationException_ReturnsNull()
    {
        // Act
        var result = ((ValidationException)null).AsProblemDetails();

        // Assert
        Assert.Null(result);
    }
}
