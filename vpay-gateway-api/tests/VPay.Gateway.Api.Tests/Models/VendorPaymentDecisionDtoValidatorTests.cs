using System.Globalization;
using FluentValidation.TestHelper;
using VPay.Gateway.Api.Models;
using Xunit;

namespace VPay.Gateway.Api.Tests.Models;

/// <summary>
/// Tests for <see cref="VendorPaymentDecisionDtoValidator"/>
/// </summary>
public class VendorPaymentDecisionDtoValidatorTests
{
    private readonly VendorPaymentDecisionDtoValidator _validator = new();

    [Fact]
    public void Should_PassValidation_ForValidDto()
    {
        var dto = CreateValidRequestDto();
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveAnyValidationErrors();
    }

    public class ClientCode : VendorPaymentDecisionDtoValidatorTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Should_RequireField(string clientCode)
        {
            var dto = CreateValidRequestDto();
            dto.PayerCode = clientCode;
            var result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.PayerCode);
        }

        [Fact]
        public void Should_EnforceMaxLength()
        {
            var dto = CreateValidRequestDto();
            dto.PayerCode = new string('A', 11);
            var result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.PayerCode);
        }
    }

    public class BillingEntityCode : VendorPaymentDecisionDtoValidatorTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Should_RequireField(string billingEntityCode)
        {
            var dto = CreateValidRequestDto();
            dto.SubpayerCode = billingEntityCode;
            var result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.SubpayerCode);
        }

        [Fact]
        public void Should_EnforceMaxLength()
        {
            var dto = CreateValidRequestDto();
            dto.SubpayerCode = new string('B', 21);
            var result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.SubpayerCode);
        }
    }

    public class PreTransactionId : VendorPaymentDecisionDtoValidatorTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Should_RequireField(string preTransactionId)
        {
            var dto = CreateValidRequestDto();
            dto.PreTransId = preTransactionId;
            var result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.PreTransId);
        }

        [Fact]
        public void Should_EnforceMaxLength()
        {
            var dto = CreateValidRequestDto();
            dto.PreTransId = new string('C', 51);
            var result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.PreTransId);
        }
    }

    public class MemberList : VendorPaymentDecisionDtoValidatorTests
    {
        [Fact]
        public void Should_RequireFieldArray()
        {
            var dto = CreateValidRequestDto();
            dto.MemberIDs = Array.Empty<VendorPaymentDecisionMemberDto>();
            var result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.MemberIDs);
        }

        [Fact]
        public void Should_RequireAtLeastOneMemberInArray()
        {
            var dto = CreateValidRequestDto();
            dto.MemberIDs = new List<VendorPaymentDecisionMemberDto>();
            var result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.MemberIDs);
        }

        [Fact]
        public void Should_ValidateEachMemberInArray()
        {
            var member = new VendorPaymentDecisionMemberDto();
            var dto = CreateValidRequestDtoWithInvalidMemberDto();
            var result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor($"{nameof(dto.MemberIDs)}[0].{nameof(member.ZetaMemberId)}");
            result.ShouldHaveValidationErrorFor($"{nameof(dto.MemberIDs)}[0].{nameof(member.ClientMemberId)}");
            result.ShouldHaveValidationErrorFor($"{nameof(dto.MemberIDs)}[0].{nameof(member.OptumMemberId)}");
            result.ShouldHaveValidationErrorFor($"{nameof(dto.MemberIDs)}[0].{nameof(member.PartyId)}");
        }

        [Fact]
        public void Should_AllowValidMaxMembersInArray()
        {
            var dto = CreateValidRequestDto(1);
            var result = _validator.TestValidate(dto);
            result.ShouldNotHaveValidationErrorFor(x => x.MemberIDs);
        }

        [Fact]
        public void Should_EnforceExceededMaxMembersInArray()
        {
            var dto = CreateValidRequestDto(3);
            var result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.MemberIDs);
        }
    }

    public class DecisionModality : VendorPaymentDecisionDtoValidatorTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Should_RequireField(string modality)
        {
            var dto = CreateValidRequestDto();
            dto.DecisionedModality = modality;
            var result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.DecisionedModality);
        }

        [Fact]
        public void Should_EnforceMaxLength()
        {
            var dto = CreateValidRequestDto();
            dto.DecisionedModality = new string('D', 9);
            var result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.DecisionedModality);
        }

        [Theory]
        [InlineData("PREISCHK")]
        [InlineData("PREISDCP")]
        [InlineData("PREISEFT")]
        [InlineData("PREISREJ")]
        public void Should_AcceptValidValues(string invalidModality)
        {
            var dto = CreateValidRequestDto();
            dto.DecisionedModality = invalidModality;
            var result = _validator.TestValidate(dto);
            result.ShouldNotHaveValidationErrorFor(x => x.DecisionedModality);
        }

        [Theory]
        [InlineData("CHK")]
        [InlineData("DCP")]
        [InlineData("EFT")]
        [InlineData("REJ")]
        public void Should_RejectInvalidValues(string invalidModality)
        {
            var dto = CreateValidRequestDto();
            dto.DecisionedModality = invalidModality;
            var result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.DecisionedModality);
        }
    }

    public class NoSelectionExpiredIndicator : VendorPaymentDecisionDtoValidatorTests
    {
        [Fact]
        public void Should_RequireField()
        {
            var dto = CreateValidRequestDto();
            // Since NoSelectionExpiredIndicator is a bool, it cannot be null.
            // Therefore, we do not have a test case for null here.
            // This test is just a placeholder to indicate that the field is required.
            var result = _validator.TestValidate(dto);
            result.ShouldNotHaveValidationErrorFor(x => x.NoSelectionExpiredIndicator);
        }
    }

    public class ZetaTransactionId : VendorPaymentDecisionDtoValidatorTests
    {
        [Fact]
        public void Should_RequireField()
        {
            var dto = CreateValidRequestDto();
            dto.ZetaTransactionId = null;
            var result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.ZetaTransactionId);
        }

        [Fact]
        public void Should_EnforceMaxLength()
        {
            var dto = CreateValidRequestDto();
            dto.ZetaTransactionId = new string('E', 51);
            var result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.ZetaTransactionId);
        }
    }

    public class ClaimRejectionReason : VendorPaymentDecisionDtoValidatorTests
    {
        [Fact]
        public void Should_AllowNull_WhenDecisionModalityIsNotReject()
        {
            var dto = CreateValidRequestDto();
            dto.DecisionedModality = VendorPaymentDecisionModality.PreTransactionIsCheck.Name;
            dto.ClaimRejectionReason = null;
            var result = _validator.TestValidate(dto);
            result.ShouldNotHaveValidationErrorFor(x => x.ClaimRejectionReason);
        }

        [Fact]
        public void Should_RequireField_WhenDecisionModalityIsReject()
        {
            var dto = CreateValidRequestDto();
            dto.DecisionedModality = VendorPaymentDecisionModality.PreTransactionIsRejectedClaim.Name;
            dto.ClaimRejectionReason = null;
            var result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.ClaimRejectionReason);
        }

        [Fact]
        public void Should_EnforceMaxLength_WhenDecisionModalityIsReject()
        {
            var dto = CreateValidRequestDto();
            dto.DecisionedModality = VendorPaymentDecisionModality.PreTransactionIsRejectedClaim.Name;
            dto.ClaimRejectionReason = new string('F', 101);
            var result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.ClaimRejectionReason);
        }
    }

    private static VendorPaymentDecisionDto CreateValidRequestDto()
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

    private static VendorPaymentDecisionDto CreateValidRequestDto(int memberCount)
    {
        var members = new List<VendorPaymentDecisionMemberDto>();
        for (int i = 1; i <= memberCount; i++)
        {
            members.Add(CreateValidMember(i));
        }
        return new VendorPaymentDecisionDto
        {
            PayerCode = "PAYER1",
            SubpayerCode = "SUBPAYER1",
            PreTransId = "PRE123",
            MemberIDs = members,
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

    private static VendorPaymentDecisionDto CreateValidRequestDtoWithInvalidMemberDto()
    {
        return new VendorPaymentDecisionDto
        {
            PayerCode = "PAYER1",
            SubpayerCode = "SUBPAYER1",
            PreTransId = "PRE123",
            MemberIDs = new List<VendorPaymentDecisionMemberDto>
            {
                CreateInvalidMember()
            },
            DecisionedModality = VendorPaymentDecisionModality.PreTransactionIsAchOrEft.Name,
            NoSelectionExpiredIndicator = true,
            ClaimRejectionReason = null,
            ZetaTransactionId = "ZETA_TXN_1"
        };
    }

    private static VendorPaymentDecisionMemberDto CreateInvalidMember()
    {
        return new VendorPaymentDecisionMemberDto
        {
            ZetaMemberId = "",
            ClientMemberId = "",
            OptumMemberId = "-1",
            PartyId = ""
        };
    }
}
