using System.Globalization;
using FluentValidation.TestHelper;
using VPay.Gateway.Common.Preferencing.Models;

namespace VPay.Gateway.Common.Preferencing.Tests.Models;

/// <summary>
/// Tests for <see cref="NetworkEnrollmentItemDto"/>
/// </summary>
public class NetworkEnrollmentItemDtoValidatorTests
{
    private static readonly NetworkEnrollmentListDto _tin = new() { ProviderTin = "123456789" };
    private readonly NetworkEnrollmentItemDtoValidator _validator = new(_tin);
    private const string DateFormat = "yyyy-MM-dd";

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("    ")]
    public void ProviderNpi_WhenEmpty_ShouldNotHaveError(string value)
    {
        var item = new NetworkEnrollmentItemDto
        {
            Npi = value
        };
        var result = _validator.TestValidate(item);
        result.ShouldNotHaveValidationErrorFor(x => x.Npi);
    }

    [Fact]
    public void ProviderNpi_WhenTooLong_ShouldHaveError()
    {
        var item = new NetworkEnrollmentItemDto
        {
            Npi = new string('1', 11)
        };
        var result = _validator.TestValidate(item);
        result.ShouldHaveValidationErrorFor(x => x.Npi);
    }

    [Theory]
    [InlineData("ABC")]
    [InlineData("123456789012")]
    [InlineData("-1")]
    public void ProviderNpi_WhenInvalid_ShouldHaveError(string value)
    {
        var item = new NetworkEnrollmentItemDto
        {
            Npi = value
        };
        var result = _validator.TestValidate(item);
        result.ShouldHaveValidationErrorFor(x => x.Npi);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("123")]
    [InlineData("1234567890")]
    public void ProviderNpi_WhenValid_ShouldNotHaveError(string value)
    {
        var item = new NetworkEnrollmentItemDto
        {
            Npi = value
        };
        var result = _validator.TestValidate(item);
        result.ShouldNotHaveValidationErrorFor(x => x.Npi);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("    ")]
    public void ProviderAddress1_WhenEmpty_ShouldHaveError(string value)
    {
        var item = new NetworkEnrollmentItemDto
        {
            Address1 = value
        };
        var result = _validator.TestValidate(item);
        result.ShouldHaveValidationErrorFor(x => x.Address1);
    }

    [Fact]
    public void ProviderAddress1_WhenTooLong_ShouldHaveError()
    {
        var item = new NetworkEnrollmentItemDto
        {
            Address1 = new string('A', 51)
        };
        var result = _validator.TestValidate(item);
        result.ShouldHaveValidationErrorFor(x => x.Address1);
    }

    [Fact]
    public void ProviderAddress1_WhenValid_ShouldNotHaveError()
    {
        var item = new NetworkEnrollmentItemDto
        {
            Address1 = "123 SOME ST"
        };
        var result = _validator.TestValidate(item);
        result.ShouldNotHaveValidationErrorFor(x => x.Address1);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("    ")]
    public void ProviderAddress2_WhenEmpty_ShouldNotHaveError(string value)
    {
        var item = new NetworkEnrollmentItemDto
        {
            Address2 = value
        };
        var result = _validator.TestValidate(item);
        result.ShouldNotHaveValidationErrorFor(x => x.Address2);
    }

    [Fact]
    public void ProviderAddress2_WhenTooLong_ShouldHaveError()
    {
        var item = new NetworkEnrollmentItemDto
        {
            Address2 = new string('A', 51)
        };
        var result = _validator.TestValidate(item);
        result.ShouldHaveValidationErrorFor(x => x.Address2);
    }

    [Fact]
    public void ProviderAddress2_WhenValid_ShouldNotHaveError()
    {
        var item = new NetworkEnrollmentItemDto
        {
            Address2 = "123 SOME ST"
        };
        var result = _validator.TestValidate(item);
        result.ShouldNotHaveValidationErrorFor(x => x.Address2);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("    ")]
    public void ProviderCity_WhenEmpty_ShouldHaveError(string value)
    {
        var item = new NetworkEnrollmentItemDto
        {
            City = value
        };
        var result = _validator.TestValidate(item);
        result.ShouldHaveValidationErrorFor(x => x.City);
    }

    [Fact]
    public void ProviderCity_WhenTooLong_ShouldHaveError()
    {
        var item = new NetworkEnrollmentItemDto
        {
            City = new string('A', 31)
        };
        var result = _validator.TestValidate(item);
        result.ShouldHaveValidationErrorFor(x => x.City);
    }

    [Fact]
    public void ProviderCity_WhenValid_ShouldNotHaveError()
    {
        var item = new NetworkEnrollmentItemDto
        {
            City = "PLANO"
        };
        var result = _validator.TestValidate(item);
        result.ShouldNotHaveValidationErrorFor(x => x.City);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("    ")]
    public void ProviderState_WhenEmpty_ShouldHaveError(string value)
    {
        var item = new NetworkEnrollmentItemDto
        {
            State = value
        };
        var result = _validator.TestValidate(item);
        result.ShouldHaveValidationErrorFor(x => x.State);
    }

    [Fact]
    public void ProviderState_WhenTooLong_ShouldHaveError()
    {
        var item = new NetworkEnrollmentItemDto
        {
            State = new string('A', 3)
        };
        var result = _validator.TestValidate(item);
        result.ShouldHaveValidationErrorFor(x => x.State);
    }

    [Fact]
    public void ProviderState_WhenValid_ShouldNotHaveError()
    {
        var item = new NetworkEnrollmentItemDto
        {
            State = "TX"
        };
        var result = _validator.TestValidate(item);
        result.ShouldNotHaveValidationErrorFor(x => x.State);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("    ")]
    public void ProviderZip9_WhenEmpty_ShouldHaveError(string value)
    {
        var item = new NetworkEnrollmentItemDto
        {
            Zip9 = value
        };
        var result = _validator.TestValidate(item);
        result.ShouldHaveValidationErrorFor(x => x.Zip9);
    }

    [Fact]
    public void ProviderZip9_WhenTooLong_ShouldHaveError()
    {
        var item = new NetworkEnrollmentItemDto
        {
            Zip9 = new string('1', 10)
        };
        var result = _validator.TestValidate(item);
        result.ShouldHaveValidationErrorFor(x => x.Zip9);
    }

    [Theory]
    [InlineData("7500")]
    [InlineData("75000-0")]
    [InlineData("ABC-123")]
    public void ProviderZip9_WhenInvalid_ShouldHaveError(string value)
    {
        var item = new NetworkEnrollmentItemDto
        {
            Zip9 = value
        };
        var result = _validator.TestValidate(item);
        result.ShouldHaveValidationErrorFor(x => x.Zip9);
    }

    [Theory]
    [InlineData("75000")]
    [InlineData("75000-0000")]
    [InlineData("750000000")]
    public void ProviderZip9_WhenValid_ShouldNotHaveError(string value)
    {
        var item = new NetworkEnrollmentItemDto
        {
            Zip9 = value
        };
        var result = _validator.TestValidate(item);
        result.ShouldNotHaveValidationErrorFor(x => x.Zip9);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("    ")]
    public void EnrollmentType_WhenEmpty_ShouldHaveError(string value)
    {
        var item = new NetworkEnrollmentItemDto
        {
            EnrollmentType = value
        };
        var result = _validator.TestValidate(item);
        result.ShouldHaveValidationErrorFor(x => x.EnrollmentType);
    }

    [Theory]
    [InlineData("ABCDEFG")]
    [InlineData("ABC123")]
    public void EnrollmentType_WhenTooLong_ShouldHaveError(string value)
    {
        var item = new NetworkEnrollmentItemDto
        {
            EnrollmentType = value
        };
        var result = _validator.TestValidate(item);
        result.ShouldHaveValidationErrorFor(x => x.EnrollmentType);
    }

    [Theory]
    [InlineData("ABC")]
    [InlineData("ZYZ")]
    public void EnrollmentType_WhenInvalid_ShouldHaveError(string value)
    {
        var item = new NetworkEnrollmentItemDto
        {
            EnrollmentType = value
        };
        var result = _validator.TestValidate(item);
        result.ShouldHaveValidationErrorFor(x => x.EnrollmentType);
    }

    [Theory]
    [InlineData("B2B")]
    [InlineData("C2B")]
    [InlineData("1")]
    [InlineData("2")]
    public void EnrollmentType_WhenValid_ShouldNotHaveError(string value)
    {
        var item = new NetworkEnrollmentItemDto
        {
            EnrollmentType = value
        };
        var result = _validator.TestValidate(item);
        result.ShouldNotHaveValidationErrorFor(x => x.EnrollmentType);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("    ")]
    public void EnrollmentStatusCd_WhenEmpty_ShouldHaveError(string value)
    {
        var item = new NetworkEnrollmentItemDto
        {
            EnrollmentStatusCd = value
        };
        var result = _validator.TestValidate(item);
        result.ShouldHaveValidationErrorFor(x => x.EnrollmentStatusCd);
    }

    [Theory]
    [InlineData("ABC")]
    [InlineData("12")]
    [InlineData("Z1")]
    public void EnrollmentStatusCd_WhenTooLong_ShouldHaveError(string value)
    {
        var item = new NetworkEnrollmentItemDto
        {
            EnrollmentStatusCd = value
        };
        var result = _validator.TestValidate(item);
        result.ShouldHaveValidationErrorFor(x => x.EnrollmentStatusCd);
    }

    [Theory]
    [InlineData("X")]
    [InlineData("0")]
    [InlineData("Z")]
    public void EnrollmentStatusCd_WhenInvalid_ShouldHaveError(string value)
    {
        var item = new NetworkEnrollmentItemDto
        {
            EnrollmentStatusCd = value
        };
        var result = _validator.TestValidate(item);
        result.ShouldHaveValidationErrorFor(x => x.EnrollmentStatusCd);
    }

    [Theory]
    [InlineData("A")]
    [InlineData("I")]
    [InlineData("1")]
    [InlineData("2")]
    public void EnrollmentStatusCd_WhenValid_ShouldNotHaveError(string value)
    {
        var item = new NetworkEnrollmentItemDto
        {
            EnrollmentStatusCd = value
        };
        var result = _validator.TestValidate(item);
        result.ShouldNotHaveValidationErrorFor(x => x.EnrollmentStatusCd);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("    ")]
    public void EnrollmentEligibilityStatus_WhenEmpty_ShouldHaveError(string value)
    {
        var item = new NetworkEnrollmentItemDto
        {
            EnrollmentEligibilityStatus = value
        };
        var result = _validator.TestValidate(item);
        result.ShouldHaveValidationErrorFor(x => x.EnrollmentEligibilityStatus);
    }

    [Theory]
    [InlineData("ABC")]
    [InlineData("12")]
    [InlineData("Z1")]
    public void EnrollmentEligibilityStatus_WhenTooLong_ShouldHaveError(string value)
    {
        var item = new NetworkEnrollmentItemDto
        {
            EnrollmentEligibilityStatus = value
        };
        var result = _validator.TestValidate(item);
        result.ShouldHaveValidationErrorFor(x => x.EnrollmentEligibilityStatus);
    }

    [Theory]
    [InlineData("A")]
    [InlineData("0")]
    [InlineData("Z")]
    public void EnrollmentEligibilityStatus_WhenInvalid_ShouldHaveError(string value)
    {
        var item = new NetworkEnrollmentItemDto
        {
            EnrollmentEligibilityStatus = value
        };
        var result = _validator.TestValidate(item);
        result.ShouldHaveValidationErrorFor(x => x.EnrollmentEligibilityStatus);
    }

    [Theory]
    [InlineData("N")]
    [InlineData("Y")]
    [InlineData("1")]
    [InlineData("2")]
    public void EnrollmentEligibilityStatus_WhenValid_ShouldNotHaveError(string value)
    {
        var item = new NetworkEnrollmentItemDto
        {
            EnrollmentEligibilityStatus = value
        };
        var result = _validator.TestValidate(item);
        result.ShouldNotHaveValidationErrorFor(x => x.EnrollmentEligibilityStatus);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("    ")]
    public void EffectiveDate_WhenEmpty_ShouldHaveError(string value)
    {
        DateTime.TryParseExact(value, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date);
        var item = new NetworkEnrollmentItemDto
        {
            EffectiveDate = date
        };
        var result = _validator.TestValidate(item);
        result.ShouldHaveValidationErrorFor(x => x.EffectiveDate);
    }

    [Theory]
    [InlineData("2001-12")]
    [InlineData("2001")]
    public void EffectiveDate_WhenInvalid_ShouldHaveError(string value)
    {
        DateTime.TryParseExact(value, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date);
        var item = new NetworkEnrollmentItemDto
        {
            EffectiveDate = date
        };
        var result = _validator.TestValidate(item);
        result.ShouldHaveValidationErrorFor(x => x.EffectiveDate);
    }

    [Theory]
    [InlineData("2001-01-01")]
    [InlineData("2099-12-31")]
    public void EffectiveDate_WhenValid_ShouldNotHaveError(string value)
    {
        DateTime.TryParseExact(value, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date);
        var item = new NetworkEnrollmentItemDto
        {
            EffectiveDate = date
        };
        var result = _validator.TestValidate(item);
        result.ShouldNotHaveValidationErrorFor(x => x.EffectiveDate);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("    ")]
    public void InitialEnrollmentDate_WhenEmpty_ShouldHaveError(string value)
    {
        DateTime.TryParseExact(value, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date);
        var item = new NetworkEnrollmentItemDto
        {
            InitialEnrollmentDate = date
        };
        var result = _validator.TestValidate(item);
        result.ShouldHaveValidationErrorFor(x => x.InitialEnrollmentDate);
    }

    [Theory]
    [InlineData("2001-12")]
    [InlineData("2001")]
    public void InitialEnrollmentDate_WhenInvalid_ShouldHaveError(string value)
    {
        DateTime.TryParseExact(value, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date);
        var item = new NetworkEnrollmentItemDto
        {
            InitialEnrollmentDate = date
        };
        var result = _validator.TestValidate(item);
        result.ShouldHaveValidationErrorFor(x => x.InitialEnrollmentDate);
    }

    [Theory]
    [InlineData("2001-01-01")]
    [InlineData("2099-12-31")]
    public void InitialEnrollmentDate_WhenValid_ShouldNotHaveError(string value)
    {
        DateTime.TryParseExact(value, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date);
        var item = new NetworkEnrollmentItemDto
        {
            InitialEnrollmentDate = date
        };
        var result = _validator.TestValidate(item);
        result.ShouldNotHaveValidationErrorFor(x => x.InitialEnrollmentDate);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("    ")]
    public void LastUpdatedDate_WhenEmpty_ShouldHaveError(string value)
    {
        DateTime.TryParseExact(value, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date);
        var item = new NetworkEnrollmentItemDto
        {
            LastUpdatedUtcDate = date
        };
        var result = _validator.TestValidate(item);
        result.ShouldHaveValidationErrorFor(x => x.LastUpdatedUtcDate);
    }

    [Theory]
    [InlineData("2001-12")]
    [InlineData("2001")]
    public void LastUpdatedDate_WhenInvalid_ShouldHaveError(string value)
    {
        DateTime.TryParseExact(value, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date);
        var item = new NetworkEnrollmentItemDto
        {
            LastUpdatedUtcDate = date
        };
        var result = _validator.TestValidate(item);
        result.ShouldHaveValidationErrorFor(x => x.LastUpdatedUtcDate);
    }

    [Theory]
    [InlineData("2001-01-01")]
    [InlineData("2099-12-31")]
    public void LastUpdatedDate_WhenValid_ShouldNotHaveError(string value)
    {
        DateTime.TryParseExact(value, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date);
        var item = new NetworkEnrollmentItemDto
        {
            LastUpdatedUtcDate = date
        };
        var result = _validator.TestValidate(item);
        result.ShouldNotHaveValidationErrorFor(x => x.LastUpdatedUtcDate);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("    ")]
    public void EnrollmentStatusReason_WhenEmpty_ShouldNotHaveError(string value)
    {
        var item = new NetworkEnrollmentItemDto
        {
            EnrollmentStatusReason = value
        };
        var result = _validator.TestValidate(item);
        result.ShouldNotHaveValidationErrorFor(x => x.EnrollmentStatusReason);
    }

    [Fact]
    public void EnrollmentStatusReason_WhenTooLong_ShouldHaveError()
    {
        var item = new NetworkEnrollmentItemDto
        {
            EnrollmentStatusReason = new string('A', 51)
        };
        var result = _validator.TestValidate(item);
        result.ShouldHaveValidationErrorFor(x => x.EnrollmentStatusReason);
    }

    [Fact]
    public void EnrollmentStatusReason_WhenValid_ShouldNotHaveError()
    {
        var item = new NetworkEnrollmentItemDto
        {
            EnrollmentStatusReason = "VALID REASON"
        };
        var result = _validator.TestValidate(item);
        result.ShouldNotHaveValidationErrorFor(x => x.EnrollmentStatusReason);
    }
}
