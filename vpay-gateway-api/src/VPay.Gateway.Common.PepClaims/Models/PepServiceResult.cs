namespace VPay.Gateway.Common.PepClaims.Models;

public class PepServiceResult
{
    public List<PepServiceClaimStatus>? ReconClaimStatusDtos { get; set; }
    public List<PepServiceParty>? ReconPartyDtos { get; set; }
}
