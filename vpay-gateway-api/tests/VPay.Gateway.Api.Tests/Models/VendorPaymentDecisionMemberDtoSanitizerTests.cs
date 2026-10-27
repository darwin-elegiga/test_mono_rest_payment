using Xunit;
using VPay.Gateway.Api.Models;

namespace VPay.Gateway.Api.Tests.Models;

public class VendorPaymentDecisionMemberDtoSanitizerTests
{
    private class TestVendorPaymentDecisionMemberDto : VendorPaymentDecisionMemberDto
    {
        public TestVendorPaymentDecisionMemberDto(string zeta, string client, string optum = "1", string party = null)
        {
            ZetaMemberId = zeta;
            ClientMemberId = client;
            OptumMemberId = optum;
            PartyId = party;
        }
    }

    [Fact]
    public void Sanitize_ReturnsNull_WhenInputIsNull()
    {
        VendorPaymentDecisionMemberDto input = null;
        var result = VendorPaymentDecisionMemberDtoSanitizer.Sanitize(input);
        Assert.Null(result);
    }

    [Fact]
    public void Sanitize_RemovesControlCharacters_FromZetaAndClientMemberId()
    {
        var input = new TestVendorPaymentDecisionMemberDto("abc\r\n\t", "def\u0001\u0002", "1", "party");
        var result = VendorPaymentDecisionMemberDtoSanitizer.Sanitize(input);

        Assert.NotNull(result);
        Assert.NotEqual(input.ZetaMemberId, result.ZetaMemberId);
        Assert.NotEqual(input.ClientMemberId, result.ClientMemberId);
        Assert.Equal("1", result.OptumMemberId);
        Assert.Equal("party", result.PartyId);
    }

    [Fact]
    public void Sanitize_CopiesOptumAndPartyId_AsIs()
    {
        var input = new TestVendorPaymentDecisionMemberDto("zeta", "client", "1", "party");
        var result = VendorPaymentDecisionMemberDtoSanitizer.Sanitize(input);

        Assert.Equal("1", result.OptumMemberId);
        Assert.Equal("party", result.PartyId);
    }

    [Fact]
    public void SanitizeLog_ReturnsNull_WhenInputIsNull()
    {
        VendorPaymentDecisionMemberDto input = null;
        var result = VendorPaymentDecisionMemberDtoSanitizer.SanitizeLog(input);
        Assert.Null(result);
    }

    [Fact]
    public void SanitizeLog_ReplacesNullAndEmptyWithLogValues()
    {
        var input = new TestVendorPaymentDecisionMemberDto(null, "", "1", "party");
        var result = VendorPaymentDecisionMemberDtoSanitizer.SanitizeLog(input);

        Assert.Equal("NULL", result.ZetaMemberId);
        Assert.Equal("", result.ClientMemberId);
        Assert.Equal("1", result.OptumMemberId);
        Assert.Equal("party", result.PartyId);
    }

    [Fact]
    public void SanitizeLog_DoesNotChangeNonNullOrNonEmptyValues()
    {
        var input = new TestVendorPaymentDecisionMemberDto("zeta", "client", "1", "party");
        var result = VendorPaymentDecisionMemberDtoSanitizer.SanitizeLog(input);

        Assert.Equal("zeta", result.ZetaMemberId);
        Assert.Equal("client", result.ClientMemberId);
        Assert.Equal("1", result.OptumMemberId);
        Assert.Equal("party", result.PartyId);
    }
}
