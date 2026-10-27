using System.Threading;
using System.Threading.Tasks;
using Refit;
using Refit.Insane.PowerPack.Caching;
using Refit.Insane.PowerPack.Retry;
using VPay.Optum.Http.ConfigApi.Models;

namespace VPay.Optum.Http.ConfigApi
{
    public interface IConfigApi
    {
        [Get("/api/otb-details/clients/{clientCode}/fundingAccount/{fundingAccount}")]
        Task<OtbBillingEntityDetailDto> GetOtbBillingEntityDetails(string clientCode, string fundingAccount, CancellationToken token = default);

        [Get("/api/billing-entities/{clientCode}/{billingEntityCode}")]
        Task<BillingEntity> GetBillingEntity(string clientCode, string billingEntityCode, CancellationToken token = default);

        [Get("/api/prs-mapping/search/clientcode/{clientCode}/{billingEntityCode}")]
        Task<PrsMappingDto> GetPrsMapping(string clientCode, string billingEntityCode, CancellationToken token = default);

        [RefitRetry(retryCount: 3)]
        [RefitCache(ttlHours: 1, ttlSeconds: 0)]
        [Get("/api/prs-payer-alias-mapping/search/payerAlias/{payerAlias}")]
        Task<IApiResponse<PrsPayerAliasMappingDto>> GetPrsPayerAliasMapping(string payerAlias, CancellationToken cancellationToken);

        [RefitRetry(retryCount: 3)]
        [RefitCache(ttlHours: 4, ttlSeconds: 0)]
        [Get("/api/prs-mapping/search/optumSubPayorId/{optumSubPayorId}")]
        Task<IApiResponse<PrsSubPayerMappingDto>> GetPrsSubPayerMapping(string optumSubPayorId, CancellationToken cancellationToken);

        [RefitRetry(retryCount: 3)]
        [RefitCache(ttlHours: 1, ttlSeconds: 0)]
        [Get("/api/prs-client-config/payer-pra")]
        Task<IApiResponse<PayerPraConfigurationDto>> GetPayerPraConfiguration([Query] string originCode, [Query] string clientCode, [Query] string billingEntityCode = "", CancellationToken cancellationToken = default);

        [RefitRetry(retryCount: 3)]
        [RefitCache(ttlHours: 1, ttlSeconds: 0)]
        [Get("/api/client-boarding-config/search/{configurationName}?page=1&pageSize=50")]
        Task<IApiResponse<ClientBoardingConfigurationDto>> GetSpecificClientBoardingConfiguration(string configurationName, [Query] string originCode, [Query] string clientCode, CancellationToken cancellationToken = default);
    }
}
