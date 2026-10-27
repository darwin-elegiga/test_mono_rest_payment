namespace VPay.Gateway.Common.PepClaims.Models;

public class PartyStatusDto
{
    public long Id { get; set; }
    public long PartyId { get; set; }
    public string Status { get; set; } = "";
    public string Email { get; set; } = "";
    public DateTime Created { get; set; }
}
