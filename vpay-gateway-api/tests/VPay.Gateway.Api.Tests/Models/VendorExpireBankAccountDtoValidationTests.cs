using System.ComponentModel.DataAnnotations;
using VPay.Gateway.Api.Models;
using Xunit;

namespace VPay.Gateway.Api.Tests.Models;

public class VendorExpireBankAccountDtoValidationTests
{
    [Fact]
    public void AllProperties_Valid_NoValidationErrors()
    {
        var dto = new VendorExpireBankAccountDto
        {
            PayerCode = "PAYER",
            SubPayerCode = "SUBPAYER",
            ClientMemberId = "CLIENT",
            ZetaMemberId = "ZETA"
        };

        var results = ValidateModel(dto);

        Assert.Empty(results);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void PayerCode_Required_ValidationError(string value)
    {
        var dto = new VendorExpireBankAccountDto
        {
            PayerCode = value,
            SubPayerCode = "SUBPAYER",
            ClientMemberId = "CLIENT",
            ZetaMemberId = "ZETA"
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.ErrorMessage == "PayerCode is required.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void SubPayerCode_Required_ValidationError(string value)
    {
        var dto = new VendorExpireBankAccountDto
        {
            PayerCode = "PAYER",
            SubPayerCode = value,
            ClientMemberId = "CLIENT",
            ZetaMemberId = "ZETA"
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.ErrorMessage == "SubPayerCode is required.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ClientMemberId_Required_ValidationError(string value)
    {
        var dto = new VendorExpireBankAccountDto
        {
            PayerCode = "PAYER",
            SubPayerCode = "SUBPAYER",
            ClientMemberId = value,
            ZetaMemberId = "ZETA"
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.ErrorMessage == "ClientMemberId is required.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ZetaMemberId_Required_ValidationError(string value)
    {
        var dto = new VendorExpireBankAccountDto
        {
            PayerCode = "PAYER",
            SubPayerCode = "SUBPAYER",
            ClientMemberId = "CLIENT",
            ZetaMemberId = value
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.ErrorMessage == "ZetaMemberId is required.");
    }

    private static List<ValidationResult> ValidateModel(VendorExpireBankAccountDto dto)
    {
        var context = new ValidationContext(dto, null, null);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(dto, context, results, true);
        return results;
    }
}
