using VPay.Gateway.Api.Models;
using Xunit;

namespace VPay.Gateway.Api.Tests.Models;

/// <summary>
/// Tests for <see cref="VendorPaymentDecisionMemberDtoMapper"/> to ensure correct mapping between domain models and DTOs.
/// </summary>
public class VendorPaymentDecisionMemberDtoMapperTests
{
    [Fact]
    public void FromDto_ThrowsArgumentNullException_WhenItemIsNull()
    {
        VendorPaymentDecisionMemberDto dto = null;
        Assert.Throws<ArgumentNullException>(() => dto!.FromDto());
    }

    [Fact]
    public void FromDto_MapsPropertiesCorrectly()
    {
        var dto = new VendorPaymentDecisionMemberDto
        {
            ZetaMemberId = "Zeta123",
            ClientMemberId = "Client456",
            OptumMemberId = "123000",
            PartyId = "Party000"
        };

        var result = dto.FromDto();

        Assert.Equal(dto.ZetaMemberId, result.ZetaMemberId);
        Assert.Equal(dto.ClientMemberId, result.ClientMemberId);
        Assert.Equal(dto.OptumMemberId, result.OptumMemberId);
        Assert.Equal(dto.PartyId, result.PartyId);
    }

    [Fact]
    public void FromDtoList_ThrowsArgumentNullException_WhenItemsIsNull()
    {
        IEnumerable<VendorPaymentDecisionMemberDto> dtos = null;
        Assert.Throws<ArgumentNullException>(() => dtos!.FromDtoList().ToList());
    }

    [Fact]
    public void FromDtoList_MapsAllItemsCorrectly()
    {
        var dtos = new List<VendorPaymentDecisionMemberDto>
        {
            new() {
                ZetaMemberId = "Zeta1",
                ClientMemberId = "Client1",
                OptumMemberId = "123001",
                PartyId = "Party1"
            },
            new() {
                ZetaMemberId = "Zeta2",
                ClientMemberId = "Client2",
                OptumMemberId = "123002",
                PartyId = "Party2"
            }
        };

        var results = dtos.FromDtoList().ToList();

        Assert.Equal(2, results.Count);

        Assert.Equal("Zeta1", results[0].ZetaMemberId);
        Assert.Equal("Client1", results[0].ClientMemberId);
        Assert.Equal("123001", results[0].OptumMemberId);
        Assert.Equal("Party1", results[0].PartyId);

        Assert.Equal("Zeta2", results[1].ZetaMemberId);
        Assert.Equal("Client2", results[1].ClientMemberId);
        Assert.Equal("123002", results[1].OptumMemberId);
        Assert.Equal("Party2", results[1].PartyId);
    }
}
