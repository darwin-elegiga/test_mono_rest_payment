using System.ComponentModel.DataAnnotations;
using VPay.Gateway.Api.Models;
using Xunit;

namespace VPay.Gateway.Api.Tests.Models;

public class VendorAddBankAccountDtoTests
{
    [Fact]
    public void AllProperties_SetAndGet_ReturnsExpectedValues()
    {
        // Arrange
        var dto = new VendorAddBankAccountDto
        {
            PayerCode = "PAYER1",
            SubPayerCode = "SUBPAYER1",
            ClientMemberId = "CLIENT1",
            ZetaMemberId = "ZETA1",
            BankRoutingNumber = "123456789",
            BankAccountNumber = "987654321"
        };

        // Assert
        Assert.Equal("PAYER1", dto.PayerCode);
        Assert.Equal("SUBPAYER1", dto.SubPayerCode);
        Assert.Equal("CLIENT1", dto.ClientMemberId);
        Assert.Equal("ZETA1", dto.ZetaMemberId);
        Assert.Equal("123456789", dto.BankRoutingNumber);
        Assert.Equal("987654321", dto.BankAccountNumber);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("PAYER1", true)]
    public void PayerCode_RequiredValidation(string value, bool isValid)
    {
        var dto = new VendorAddBankAccountDto
        {
            PayerCode = value,
            SubPayerCode = "X",
            ClientMemberId = "X",
            ZetaMemberId = "X",
            BankRoutingNumber = "123456789",
            BankAccountNumber = "123"
        };

        var results = ValidateModel(dto);
        Assert.Equal(isValid, !results.Any(r => r.MemberNames.Contains(nameof(dto.PayerCode))));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("SUBPAYER1", true)]
    public void SubPayerCode_RequiredValidation(string value, bool isValid)
    {
        var dto = new VendorAddBankAccountDto
        {
            PayerCode = "X",
            SubPayerCode = value,
            ClientMemberId = "X",
            ZetaMemberId = "X",
            BankRoutingNumber = "123456789",
            BankAccountNumber = "123"
        };

        var results = ValidateModel(dto);
        Assert.Equal(isValid, !results.Any(r => r.MemberNames.Contains(nameof(dto.SubPayerCode))));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("CLIENT1", true)]
    public void ClientMemberId_RequiredValidation(string value, bool isValid)
    {
        var dto = new VendorAddBankAccountDto
        {
            PayerCode = "X",
            SubPayerCode = "X",
            ClientMemberId = value,
            ZetaMemberId = "X",
            BankRoutingNumber = "123456789",
            BankAccountNumber = "123"
        };

        var results = ValidateModel(dto);
        Assert.Equal(isValid, !results.Any(r => r.MemberNames.Contains(nameof(dto.ClientMemberId))));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("ZETA1", true)]
    public void ZetaMemberId_RequiredValidation(string value, bool isValid)
    {
        var dto = new VendorAddBankAccountDto
        {
            PayerCode = "X",
            SubPayerCode = "X",
            ClientMemberId = "X",
            ZetaMemberId = value,
            BankRoutingNumber = "123456789",
            BankAccountNumber = "123"
        };

        var results = ValidateModel(dto);
        Assert.Equal(isValid, !results.Any(r => r.MemberNames.Contains(nameof(dto.ZetaMemberId))));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("123456789", true)]
    [InlineData("1234567890", false)] // Exceeds max length
    public void BankRoutingNumber_Validation(string value, bool isValid)
    {
        var dto = new VendorAddBankAccountDto
        {
            PayerCode = "X",
            SubPayerCode = "X",
            ClientMemberId = "X",
            ZetaMemberId = "X",
            BankRoutingNumber = value,
            BankAccountNumber = "123"
        };

        var results = ValidateModel(dto);
        Assert.Equal(isValid, !results.Any(r => r.MemberNames.Contains(nameof(dto.BankRoutingNumber))));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("12345678901234567890123456789012", true)] // 32 chars
    [InlineData("123456789012345678901234567890123", false)] // 33 chars
    public void BankAccountNumber_Validation(string value, bool isValid)
    {
        var dto = new VendorAddBankAccountDto
        {
            PayerCode = "X",
            SubPayerCode = "X",
            ClientMemberId = "X",
            ZetaMemberId = "X",
            BankRoutingNumber = "123456789",
            BankAccountNumber = value
        };

        var results = ValidateModel(dto);
        Assert.Equal(isValid, !results.Any(r => r.MemberNames.Contains(nameof(dto.BankAccountNumber))));
    }

    private static List<ValidationResult> ValidateModel(object model)
    {
        var context = new ValidationContext(model, null, null);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, context, results, true);
        return results;
    }
}
