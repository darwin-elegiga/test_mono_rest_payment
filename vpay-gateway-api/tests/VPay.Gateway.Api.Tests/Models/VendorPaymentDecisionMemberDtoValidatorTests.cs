using System.Globalization;
using FluentValidation.TestHelper;
using VPay.Gateway.Api.Models;
using Xunit;

namespace VPay.Gateway.Api.Tests.Models;

/// <summary>
/// Tests for <see cref="VendorPaymentDecisionMemberDtoValidator"/>
/// </summary>
public class VendorPaymentDecisionMemberDtoValidatorTests
{
    private static VendorPaymentDecisionDto CreateValidEftRequestDto()
    {
        return new VendorPaymentDecisionDto
        {
            PayerCode = "PAYER1",
            SubpayerCode = "SUBPAYER1",
            PreTransId = "PRE123",
            MemberIDs = new List<VendorPaymentDecisionMemberDto>
            {
                CreateValidMember(1)
            },
            DecisionedModality = VendorPaymentDecisionModality.PreTransactionIsAchOrEft.Name,
            NoSelectionExpiredIndicator = true,
            ClaimRejectionReason = null,
            ZetaTransactionId = "ZETA_TXN_1"
        };
    }

    private static VendorPaymentDecisionDto CreateValidCheckRequestDto()
    {
        return new VendorPaymentDecisionDto
        {
            PayerCode = "PAYER1",
            SubpayerCode = "SUBPAYER1",
            PreTransId = "PRE123",
            MemberIDs = new List<VendorPaymentDecisionMemberDto>
            {
                CreateValidMember(1)
            },
            DecisionedModality = VendorPaymentDecisionModality.PreTransactionIsCheck.Name,
            NoSelectionExpiredIndicator = true,
            ClaimRejectionReason = null,
            ZetaTransactionId = "ZETA_TXN_1"
        };
    }

    private static VendorPaymentDecisionMemberDto CreateValidMember(int index)
    {
        return new VendorPaymentDecisionMemberDto
        {
            ZetaMemberId = $"ZETA{index}",
            ClientMemberId = $"CLIENT{index}",
            OptumMemberId = $"{12345 + index}",
            PartyId = $"{index}"
        };
    }

    [Fact]
    public void Should_PassValidation_ForValidMember()
    {
        var member = CreateValidMember(1);
        var validator = new VendorPaymentDecisionMemberDtoValidator(CreateValidEftRequestDto());
        var result = validator.TestValidate(member);
        result.ShouldNotHaveAnyValidationErrors();
    }

    public class ZetaMemberId : VendorPaymentDecisionMemberDtoValidatorTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Should_RequireField_WhenModalityIsAchOrEft(string zetaMemberId)
        {
            var member = CreateValidMember(1);
            member.ZetaMemberId = zetaMemberId;
            var validator = new VendorPaymentDecisionMemberDtoValidator(CreateValidEftRequestDto());
            var result = validator.TestValidate(member);
            result.ShouldHaveValidationErrorFor(x => x.ZetaMemberId);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Should_RequireField_WhenModalityIsNotAchOrEft(string zetaMemberId)
        {
            var member = CreateValidMember(1);
            member.ZetaMemberId = zetaMemberId;
            var validator = new VendorPaymentDecisionMemberDtoValidator(CreateValidCheckRequestDto());
            var result = validator.TestValidate(member);
            result.ShouldHaveValidationErrorFor(x => x.ZetaMemberId);
        }

        [Fact]
        public void Should_EnforceMaxLength()
        {
            var member = CreateValidMember(1);
            member.ZetaMemberId = new string('A', 51);
            var validator = new VendorPaymentDecisionMemberDtoValidator(CreateValidEftRequestDto());
            var result = validator.TestValidate(member);
            result.ShouldHaveValidationErrorFor(x => x.ZetaMemberId);
        }
    }

    public class ClientMemberId : VendorPaymentDecisionMemberDtoValidatorTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Should_RequireField_WhenModalityIsNotAchOrEft(string clientMemberId)
        {
            var member = CreateValidMember(1);
            member.ClientMemberId = clientMemberId;
            var validator = new VendorPaymentDecisionMemberDtoValidator(CreateValidCheckRequestDto());
            var result = validator.TestValidate(member);
            result.ShouldHaveValidationErrorFor(x => x.ClientMemberId);
        }

        [Fact]
        public void Should_RequireField_WhenModalityIsAchOrEft()
        {
            var member = CreateValidMember(1);
            member.ClientMemberId = null;
            var validator = new VendorPaymentDecisionMemberDtoValidator(CreateValidEftRequestDto());
            var result = validator.TestValidate(member);
            result.ShouldHaveValidationErrorFor(x => x.ClientMemberId);
        }

        [Fact]
        public void Should_EnforceMaxLength()
        {
            var member = CreateValidMember(1);
            member.ClientMemberId = new string('B', 51);
            var validator = new VendorPaymentDecisionMemberDtoValidator(CreateValidEftRequestDto());
            var result = validator.TestValidate(member);
            result.ShouldHaveValidationErrorFor(x => x.ClientMemberId);
        }
    }

    public class OptumMemberId : VendorPaymentDecisionMemberDtoValidatorTests
    {
        [Fact]
        public void Should_NotRequireField_WhenModalityIsNotAchOrEft()
        {
            var member = CreateValidMember(1);
            member.OptumMemberId = "0";
            var validator = new VendorPaymentDecisionMemberDtoValidator(CreateValidCheckRequestDto());
            var result = validator.TestValidate(member);
            result.ShouldNotHaveValidationErrorFor(x => x.OptumMemberId);
        }

        [Fact]
        public void Should_RequireField_WhenModalityIsAchOrEft()
        {
            var member = CreateValidMember(1);
            member.OptumMemberId = "0";
            var validator = new VendorPaymentDecisionMemberDtoValidator(CreateValidEftRequestDto());
            var result = validator.TestValidate(member);
            result.ShouldHaveValidationErrorFor(x => x.OptumMemberId);
        }

        [Fact]
        public void Should_RequireGreaterThanZero()
        {
            var member = CreateValidMember(1);
            member.OptumMemberId = "0";
            var validator = new VendorPaymentDecisionMemberDtoValidator(CreateValidEftRequestDto());
            var result = validator.TestValidate(member);
            result.ShouldHaveValidationErrorFor(x => x.OptumMemberId);
        }

        [Fact]
        public void Should_PassValidation_ForPositiveValue()
        {
            var member = CreateValidMember(1);
            member.OptumMemberId = "1";
            var validator = new VendorPaymentDecisionMemberDtoValidator(CreateValidEftRequestDto());
            var result = validator.TestValidate(member);
            result.ShouldNotHaveValidationErrorFor(x => x.OptumMemberId);
        }
    }

    public class PartyId : VendorPaymentDecisionMemberDtoValidatorTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Should_RequireFieldModalityIsNotAchOrEft(string partyId)
        {
            var member = CreateValidMember(1);
            member.PartyId = partyId;
            var validator = new VendorPaymentDecisionMemberDtoValidator(CreateValidCheckRequestDto());
            var result = validator.TestValidate(member);
            result.ShouldHaveValidationErrorFor(x => x.PartyId);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Should_RequireFieldModalityIsAchOrEft(string partyId)
        {
            var member = CreateValidMember(1);
            member.PartyId = partyId;
            var validator = new VendorPaymentDecisionMemberDtoValidator(CreateValidEftRequestDto());
            var result = validator.TestValidate(member);
            result.ShouldHaveValidationErrorFor(x => x.PartyId);
        }

        [Fact]
        public void Should_EnforceMaxLength()
        {
            var member = CreateValidMember(1);
            member.PartyId = new string('C', 51);
            var validator = new VendorPaymentDecisionMemberDtoValidator(CreateValidEftRequestDto());
            var result = validator.TestValidate(member);
            result.ShouldHaveValidationErrorFor(x => x.PartyId);
        }
    }
}
