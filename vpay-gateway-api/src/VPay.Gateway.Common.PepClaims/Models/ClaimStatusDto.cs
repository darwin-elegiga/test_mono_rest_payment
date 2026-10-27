namespace VPay.Gateway.Common.PepClaims.Models;

public class ClaimStatusDto
{
    public long Id { get; set; }
    public string ClaimId { get; set; } = "";
    public string BPMInstanceId { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime Created { get; set; }
}
