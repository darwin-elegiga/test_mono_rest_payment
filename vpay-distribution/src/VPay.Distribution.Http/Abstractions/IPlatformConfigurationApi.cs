using Refit;
using Refit.Insane.PowerPack.Caching;
using Refit.Insane.PowerPack.Retry;
using VPay.Distribution.Http.Abstractions.Dto;

namespace VPay.Distribution.Http.Abstractions
{
    public interface IPlatformConfigurationApi
    {
        [Get("/api/about")]
        Task GetAbout();

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
            string? originCode = null,
            string? billingEntityCode = null,
            string? paymentTypeCode = null,
            DateTime? configurationDate = null,
            CancellationToken cancellationToken = default);
    }
}
