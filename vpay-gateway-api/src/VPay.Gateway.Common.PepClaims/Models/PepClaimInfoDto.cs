namespace VPay.Gateway.Common.PepClaims.Models;

public class PepClaimInfoDto : PepClaimDto
{
    public ICollection<PartyDto> Parties { get; set; } = new List<PartyDto>();
    public ICollection<ClaimStatusDto> ClaimStatuses { get; set; } = new List<ClaimStatusDto>();
}
