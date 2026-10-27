using FluentValidation.TestHelper;
using VPay.Gateway.Api.Models;
using Xunit;

namespace VPay.Gateway.Api.Tests.Models;

public class PaymentRejectionRequestDtoValidatorTests
{
    private readonly PaymentRejectionRequestDtoValidator _validator = new();

    private const string NetworkName = "VALID-NAME";
    private const long TransactionId = 1234567890;
    private const string RejectionCode = "UTLA";
    private const string RejectionReason = "Unable to Locate Account";

    [Fact]
    public void WithValidRequest_ShouldNotError()
    {
        var dto = new PaymentRejectionRequestDto
        {
            NetworkPartner = NetworkName,
            OptumTransactionId = TransactionId,
            RejectionReasonCode = RejectionCode,
            RejectionReason = RejectionReason
        };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("    ")]
    public void NetworkPartner_WhenEmpty_ShouldHaveError(string value)
    {
        var item = new PaymentRejectionRequestDto
        {
            NetworkPartner = value
        };
        var result = _validator.TestValidate(item);
        result.ShouldHaveValidationErrorFor(x => x.NetworkPartner);
    }

    [Fact]
    public void NetworkPartner_WhenTooLong_ShouldHaveError()
    {
        var item = new PaymentRejectionRequestDto
        {
            NetworkPartner = new string('A', 21)
        };
        var result = _validator.TestValidate(item);
        result.ShouldHaveValidationErrorFor(x => x.NetworkPartner);
    }

    [Fact]
    public void NetworkPartner_WhenValid_ShouldNotHaveError()
    {
        var item = new PaymentRejectionRequestDto
        {
            NetworkPartner = "VALID-NAME"
        };
        var result = _validator.TestValidate(item);
        result.ShouldNotHaveValidationErrorFor(x => x.NetworkPartner);
    }

    [Fact]
    public void TransactionId_WhenEmpty_ShouldHaveError()
    {
        var item = new PaymentRejectionRequestDto();
        var result = _validator.TestValidate(item);
        result.ShouldHaveValidationErrorFor(x => x.OptumTransactionId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void TransactionId_WhenZeroOrNegative_ShouldHaveError(long value)
    {
        var item = new PaymentRejectionRequestDto
        {
            OptumTransactionId = value
        };
        var result = _validator.TestValidate(item);
        result.ShouldHaveValidationErrorFor(x => x.OptumTransactionId);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(long.MaxValue)]
    public void TransactionId_WhenValid_ShouldNotHaveError(long value)
    {
        var item = new PaymentRejectionRequestDto
        {
            OptumTransactionId = value
        };
        var result = _validator.TestValidate(item);
        result.ShouldNotHaveValidationErrorFor(x => x.OptumTransactionId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("    ")]
    public void RejectionCode_WhenEmpty_ShouldHaveError(string value)
    {
        var item = new PaymentRejectionRequestDto
        {
            RejectionReasonCode = value
        };
        var result = _validator.TestValidate(item);
        result.ShouldHaveValidationErrorFor(x => x.RejectionReasonCode);
    }

    [Fact]
    public void RejectionCode_WhenTooLong_ShouldHaveError()
    {
        var item = new PaymentRejectionRequestDto
        {
            RejectionReasonCode = new string('A', 5)
        };
        var result = _validator.TestValidate(item);
        result.ShouldHaveValidationErrorFor(x => x.RejectionReasonCode);
    }

    [Theory]
    [InlineData("A123")]
    [InlineData("ABCD")]
    [InlineData("AB01")]
    [InlineData("WXYZ")]
    public void RejectionCode_WhenInvalid_ShouldHaveError(string value)
    {
        var item = new PaymentRejectionRequestDto
        {
            RejectionReasonCode = value
        };
        var result = _validator.TestValidate(item);
        result.ShouldHaveValidationErrorFor(x => x.RejectionReasonCode);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("12")]
    [InlineData("123")]
    [InlineData("1234")]
    [InlineData("0000")]
    [InlineData("9999")]
    public void RejectionCode_WhenNumeric_ShouldHaveError(string value)
    {
        var item = new PaymentRejectionRequestDto
        {
            RejectionReasonCode = value
        };
        var result = _validator.TestValidate(item);
        result.ShouldHaveValidationErrorFor(x => x.RejectionReasonCode);
    }

    [Theory]
    [InlineData("ZBA0")]
    [InlineData("UTP0")]
    [InlineData("ITP0")]
    [InlineData("UTLA")]
    [InlineData("DPCB")]
    [InlineData("ACOC")]
    [InlineData("CNGR")]
    [InlineData("RRJD")]
    public void RejectionCode_WhenValid_ShouldNotHaveError(string value)
    {
        var item = new PaymentRejectionRequestDto
        {
            RejectionReasonCode = value
        };
        var result = _validator.TestValidate(item);
        result.ShouldNotHaveValidationErrorFor(x => x.RejectionReasonCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("    ")]
    public void RejectionReason_WhenEmpty_ShouldHaveError(string value)
    {
        var item = new PaymentRejectionRequestDto
        {
            RejectionReason = value
        };
        var result = _validator.TestValidate(item);
        result.ShouldHaveValidationErrorFor(x => x.RejectionReason);
    }

    [Fact]
    public void RejectionReason_WhenTooLong_ShouldHaveError()
    {
        var item = new PaymentRejectionRequestDto
        {
            RejectionReason = new string('A', 257)
        };
        var result = _validator.TestValidate(item);
        result.ShouldHaveValidationErrorFor(x => x.RejectionReason);
    }

    [Fact]
    public void RejectionReason_WhenValid_ShouldNotHaveError()
    {
        var item = new PaymentRejectionRequestDto
        {
            RejectionReason = "Unable to Locate Account"
        };
        var result = _validator.TestValidate(item);
        result.ShouldNotHaveValidationErrorFor(x => x.RejectionReason);
    }
}
