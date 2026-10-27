using System;
using System.Threading;
using System.Threading.Tasks;
using Refit;
using Refit.Insane.PowerPack.Caching;
using Refit.Insane.PowerPack.Retry;
using VPay.DocSys.Parsing.Http.Abstractions.PlatformConfiguration.Dto;

namespace VPay.DocSys.Parsing.Http.Abstractions.PlatformConfiguration
{
    public interface IPlatformConfigurationApi
    {
        [Get("/api/about")]
        Task GetAbout();

        [RefitRetry(retryCount:3)]
        [RefitCache(ttlHours: 4, ttlSeconds: 0)]
        [Get("/api/client-boarding-config/search/{configurationName}")]
        Task<Pagination<TDto>> GetClientBoardingConfigs<TDto>(
            [RefitCachePrimaryKey] string configurationName,
            [RefitCachePrimaryKey] string originCode = "",
            [RefitCachePrimaryKey] string clientCode = "",
            [RefitCachePrimaryKey] string billingEntityCode = "",
            [RefitCachePrimaryKey] string paymentTypeCode = "",
            [RefitCachePrimaryKey] DateTimeOffset? configurationDate = null,
            [RefitCachePrimaryKey] int page = 1,
            [RefitCachePrimaryKey] int pageSize = 50,
            CancellationToken cancellationToken = default)
            where TDto : ClientBoardingConfigDto;

        [RefitRetry(retryCount: 3)]
        [RefitCache(ttlHours: 4, ttlSeconds: 0)]
        [Get("/api/client-boarding-config/search/CUSTOM_SEND_BACK")]
        Task<Pagination<CustomSendBackClientBoardingConfigDto>> GetCustomSendBackByClient(
            [RefitCachePrimaryKey] string clientCode = "",
            CancellationToken cancellationToken = default);

        [RefitRetry(retryCount: 3)]
        [RefitCache(ttlHours: 4, ttlSeconds: 0)]
        [Get("/api/client-boarding-config/search/EDI_DELIVERY")]
        Task<Pagination<EdiDeliveryClientBoardingConfigDto>> GetEdiDeliveryByClient(
            [RefitCachePrimaryKey] string clientCode = "",
            [RefitCachePrimaryKey] string billingEntityCode = "",
            CancellationToken cancellationToken = default);

        [RefitRetry(retryCount: 3)]
        [RefitCache(ttlHours: 4, ttlSeconds: 0)]
        [Get("/api/otb-details/clients/{tpaCode}/billingEntities")]
        Task<OtbBillingEntityClientDto> GetOtbDetails(
            [RefitCachePrimaryKey] string tpaCode);

        [RefitRetry(retryCount: 3)]
        [RefitCache(ttlHours: 4, ttlSeconds: 0)]
        [Get("/api/otb-details/clients/{tpaCode}/billingEntities")]
        Task<OtbBillingEntityClientDto> GetOtbDetails(
            [RefitCachePrimaryKey] string tpaCode,
            [RefitCachePrimaryKey("beCode")][Query(CollectionFormat.Multi)] string[] beCode);

        [RefitRetry(retryCount: 3)]
        [RefitCache(ttlHours: 4, ttlSeconds: 0)]
        [Get("/api/prs-mapping/search/clientcode/{clientCode}/{billingEntityCode}")]
        Task<PrsMappingResult> GetMappingByClientCodeAndBillingEntityCode(
            [RefitCachePrimaryKey] string clientCode,
            [RefitCachePrimaryKey] string billingEntityCode, CancellationToken token = default);

        [RefitRetry(retryCount: 3)]
        [RefitCache(ttlHours: 4, ttlSeconds: 0)]
        [Get("/api/networks/{networkPartner}")]
        Task<NetworkDto> GetNetworkPartner(
            string networkPartner,
            bool? activeStatus = null,
            CancellationToken cancellationToken = default);

        [RefitRetry(retryCount: 3)]
        [RefitCache(ttlHours: 4, ttlSeconds: 0)]
        [Get("/api/client-boarding-config/optum-prs")]
        Task<OptumPrsConfigurationDto> GetOptumPrsConfiguration(
            string clientCode,
            string originCode = null,
            string billingEntityCode = null,
            string paymentTypeCode = null,
            DateTime? configurationDate = null,
            CancellationToken cancellationToken = default);

        [RefitRetry(retryCount: 3)]
        [RefitCache(ttlHours: 4, ttlSeconds: 0)]
        [Get("/api/client-boarding-config/generate-document-status")]
        Task<GenerateDocumentStatusDto> GetGenerateDocumentStatus(            
            string originCode = null,
            string clientCode = null,
            string billingEntityCode = null,
            DateTime? configurationDate = null,
            CancellationToken cancellationToken = default);
    }
}
