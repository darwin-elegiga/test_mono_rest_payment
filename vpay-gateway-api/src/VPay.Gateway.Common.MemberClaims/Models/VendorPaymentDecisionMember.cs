using System.Text.Json.Serialization;

namespace VPay.Gateway.Common.MemberClaims.Models;

public class VendorPaymentDecisionMember
{
    /// <summary>
    /// The vendor member Id. Required.
    /// </summary>
    [JsonPropertyName("zetaMemberID")]
    public string ZetaMemberId { get; set; } = string.Empty;

    /// <summary>
    /// The client member Id. Required.
    /// </summary>
    [JsonPropertyName("clientMemberID")]
    public string ClientMemberId { get; set; } = string.Empty;

    /// <summary>
    /// The internal Optum member Id. Optional, required if Decision Modality is 'PREISEFT'
    /// </summary>
    [JsonPropertyName("optumMemberID")]
    public string OptumMemberId { get; set; } = string.Empty;

    /// <summary>
    /// The party Id. Required.
    /// </summary>
    [JsonPropertyName("partyID")]
    public string PartyId { get; set; } = string.Empty;
}
