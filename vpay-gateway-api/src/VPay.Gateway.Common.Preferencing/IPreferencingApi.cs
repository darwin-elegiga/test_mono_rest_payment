using Refit;
using VPay.Gateway.Common.Preferencing.Models;

namespace VPay.Gateway.Common.Preferencing;

/// <summary>
/// Interface defining the shape of the Preferencing API.
/// </summary>
public interface IPreferencingApi
{
    [Post("/api/engine/prs-decision")]
    Task<PrsDecisionResponse> PrsDecision([Body] PrsDecisionRequest prsDecisionRequest, CancellationToken token);

    [Post("/api/engine/batch-solve")]
    Task<IEnumerable<BatchSolveResponse>> BatchSolve([Body] IEnumerable<BatchSolveRequest> batchSolveRequest, CancellationToken token);

    [Get("/api/networks/{networkPartner}")]
    Task<NetworkDto> GetNetwork(string networkPartner, bool? activeStatus, CancellationToken token);

    [Get("/api/networks/{networkPartner}/clients/{clientCode}/enrollments/{billingEntityCode}")]
    Task<NetworkPayorEnrollmentDto> GetNetworkPayorEnrollment(string networkPartner, string clientCode, string billingEntityCode, DateTime? dateWhenActive, CancellationToken token);

    [Get("/api/networks/{networkPartner}/providers/{providerTin}/enrollments/{enrollmentType}")]
    Task<NetworkProviderEnrollmentDto> GetNetworkProviderEnrollment(string networkPartner, string providerTin, string enrollmentType, DateTime? dateWhenActive, CancellationToken token);

    [Post("/api/networks/{networkPartner}/providers")]
    Task<NetworkEnrollmentHeaderDto> EnrollNetworkProviders(string networkPartner, [Body] NetworkEnrollmentRequestDto entity, CancellationToken token);

    [Post("/api/Preferencing/v2/engine/monetization")]
    Task<MonetizationResponse> GetMonetizedStatus([Body] MonetizationRequest monetizedRequest, CancellationToken token);
}
