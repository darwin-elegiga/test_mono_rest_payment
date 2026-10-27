using Refit;

namespace VPay.Gateway.Common.PlatformConfig;

/// <summary>
/// Interface defining the shape of the Platform Config API
/// </summary>
public interface IPlatformConfigApi
{
    [Get("/api/origin-clients/client/{clientCode}")]
    Task<OriginClientDto?> GetOriginClient(string clientCode, CancellationToken cancellationToken);

    [Get("/api/external-client-configuration/{clientCode}?includePassword=true")]
    Task<ExternalClientConfigurationDto[]> GetExternalClientConfiguration(
        string clientCode,
        [Query(Format = "yyyy-MM-dd")] DateTime date,
        [Query] ExternalClientConfigurationType type = ExternalClientConfigurationType.Guidewire,
        CancellationToken cancellationToken = default);

    [Get("/api/billing-entities/{clientCode}/{billingEntityCode}")]
    Task<BillingEntityDto?> GetBillingEntity(string clientCode, string billingEntityCode, CancellationToken cancellationToken);

    [Get("/api/billing-entities?clientCode={clientCode}&page={page}&pageSize={pageSize}")]
    Task<Pagination<BillingEntityDto>?> GetBillingEntities(string clientCode, int page = 1, int pageSize = 500, CancellationToken cancellationToken = default);

    [Get("/api/client-boarding-config/member-payment-enrolled")]
    Task<MemberPaymentEnrolledConfigDto?> GetMemberPaymentEnrolled(string originCode, string clientCode, CancellationToken cancellationToken);

    [Get("/api/prs-mapping/search/optumPayorId/{optumPayorId}?page={page}&pageSize={pageSize}")]
    Task<Pagination<OptumSubPayerDto>?> GetOptumPayer(string optumPayorId, int page = 1, int pageSize = 500, CancellationToken cancellationToken = default);

    [Get("/api/prs-mapping/search/optumSubPayorId/{optumSubPayorId}")]
    Task<OptumSubPayerDto?> GetOptumSubPayer(string optumSubPayorId, CancellationToken cancellationToken);

    [Get("/api/client-boarding-config/optum-prs")]
    Task<OptumPrsConfigurationDto> GetOptumPrsConfiguration(string? clientCode, string? originCode = null, string? billingEntityCode = null, string? paymentTypeCode = null, DateTime? configurationDate = null, CancellationToken token = default);

    [Get("/api/client-boarding-config/search/{configurationName}?page=1&pageSize=50")]
    Task<IApiResponse<ClientBoardingConfigurationDto>> GetClientBoardingConfiguration(string configurationName, [Query] string originCode, [Query] string clientCode, CancellationToken cancellationToken = default);

    [Get("/api/otb-details/clients/{clientCode}/fundingAccount/{fundingAccount}")]
    Task<OtbBillingEntityDetailDto> GetOtbBillingEntityDetails(
        string clientCode,
        string fundingAccount,
        CancellationToken token = default);
}
