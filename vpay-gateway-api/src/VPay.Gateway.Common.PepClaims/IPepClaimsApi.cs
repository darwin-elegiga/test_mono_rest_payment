using Refit;
using VPay.Gateway.Common.PepClaims.Models;

namespace VPay.Gateway.Common.PepClaims;

public interface IPepClaimsApi
{
    [Post("/api/ReconFile")]
    Task<PepServiceResult> GetPepReconFile(PepServiceParam param, CancellationToken cancellationToken = default);

    [Get("/api/PepClaims/ClaimInformation/{claimId}")]
    Task<PepClaimInfoDto> GetClaimById(long claimId, CancellationToken cancellationToken = default);

    [Post("/api/PepClaims/ClaimInformation")]
    Task<List<PepClaimInfoDto>> GetClaimsByList([Body] List<string> claimIds, CancellationToken cancellationToken = default);
}
