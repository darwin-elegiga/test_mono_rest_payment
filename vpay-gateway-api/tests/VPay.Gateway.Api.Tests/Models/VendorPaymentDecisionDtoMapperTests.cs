using Xunit;
using VPay.Gateway.Api.Models;

namespace VPay.Gateway.Api.Tests.Models;

/// <summary>
/// Tests for <see cref="VendorPaymentDecisionDtoMapper"/> to ensure correct mapping between domain models and DTOs.
/// </summary>
public class VendorPaymentDecisionDtoMapperTests
{
    private static VendorPaymentDecisionDto CreateSampleDto()
    {
        return new VendorPaymentDecisionDto
        {
            PayerCode = "PCODE",
            SubpayerCode = "SUBPCODE",
            PreTransId = "PRE123",
            DecisionedModality = "Auto",
            NoSelectionExpiredIndicator = true,
            ClaimRejectionReason = "Reason",
            ZetaTransactionId = "ZETA123",
            MemberIDs =
            [
                new VendorPaymentDecisionMemberDto { ClientMemberId = "CM01", OptumMemberId = "123001", PartyId = "P01", ZetaMemberId = "ZM01" },
                new VendorPaymentDecisionMemberDto { ClientMemberId = "CM02", OptumMemberId = "123002", PartyId = "P02", ZetaMemberId = "ZM02" }
            ]
        };
    }

    [Fact]
    public void FromDto_ShouldMapAllPropertiesCorrectly()
    {
        // Arrange
        var dto = CreateSampleDto();

        // Act
        var result = dto.FromDto();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dto.PayerCode, result.PayerCode);
        Assert.Equal(dto.SubpayerCode, result.SubpayerCode);
        Assert.Equal(dto.PreTransId, result.PreTransId);
        Assert.Equal(dto.DecisionedModality, result.DecisionedModality);
        Assert.Equal(dto.NoSelectionExpiredIndicator, result.NoSelectionExpiredIndicator);
        Assert.Equal(dto.ClaimRejectionReason, result.ClaimRejectionReason);
        Assert.Equal(dto.ZetaTransactionId, result.ZetaTransactionId);
        Assert.NotNull(result.MemberIDs);
        Assert.Equal(dto.MemberIDs.Count, result.MemberIDs.Count);
    }

    [Fact]
    public void FromDto_ShouldThrowArgumentNullException_WhenDtoIsNull()
    {
        VendorPaymentDecisionDto dto = null;
        Assert.Throws<ArgumentNullException>(() => dto!.FromDto());
    }

    [Fact]
    public void FromDtoList_ShouldMapAllItemsCorrectly()
    {
        // Arrange
        var dtos = new List<VendorPaymentDecisionDto>
        {
            CreateSampleDto(),
            CreateSampleDto()
        };

        // Act
        var results = dtos.FromDtoList().ToList();

        // Assert
        Assert.Equal(dtos.Count, results.Count);
        for (int i = 0; i < dtos.Count; i++)
        {
            Assert.Equal(dtos[i].PayerCode, results[i].PayerCode);
            Assert.Equal(dtos[i].SubpayerCode, results[i].SubpayerCode);
            Assert.Equal(dtos[i].PreTransId, results[i].PreTransId);
            Assert.Equal(dtos[i].DecisionedModality, results[i].DecisionedModality);
            Assert.Equal(dtos[i].NoSelectionExpiredIndicator, results[i].NoSelectionExpiredIndicator);
            Assert.Equal(dtos[i].ClaimRejectionReason, results[i].ClaimRejectionReason);
            Assert.Equal(dtos[i].ZetaTransactionId, results[i].ZetaTransactionId);
        }
    }

    [Fact]
    public void FromDtoList_ShouldThrowArgumentNullException_WhenDtoIsNull()
    {
        IEnumerable<VendorPaymentDecisionDto> dtos = null;
        Assert.Throws<ArgumentNullException>(() => dtos!.FromDtoList().ToList());
    }

    [Fact]
    public void FromDtoList_ShouldThrowArgumentNullException_WhenDtoMemberListIsNull()
    {
        // Arrange
        var dto = CreateSampleDto();
        dto.MemberIDs = null;

        Assert.Throws<ArgumentNullException>(() => dto!.FromDto());
    }
}
