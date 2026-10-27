namespace VPay.Gateway.Common.PepClaims.Models;

public class PepServiceParty
{
    public string ClaimId { get; set; } = "";
    public string MemberId { get; set; } = "";
    public string? PartyEmail { get; set; }
    public string? PaymentId { get; set; }
    public string? Phone1 { get; set; }
    public string? Phone1Type { get; set; }
    public string? Phone2 { get; set; }
    public string? Phone2Type { get; set; }
    public string? PartyStatus { get; set; }
    public DateTime? PartyStatusCreated { get; set; }
    public string? PartyName { get; set; }
}
