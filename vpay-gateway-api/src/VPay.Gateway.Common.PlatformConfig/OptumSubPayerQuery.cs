using System.Net;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Refit;
using VPay.Extensions.Caching.Memory;

namespace VPay.Gateway.Common.PlatformConfig;

public record OptumSubPayerQuery(string OptumSubPayerId) : IRequest<OptumSubPayerDto?>;

public class OptumSubPayerQueryHandler : IRequestHandler<OptumSubPayerQuery, OptumSubPayerDto?>
{
    private readonly IPlatformConfigApi _platformConfigApi;
    private readonly IMemoryCache<OptumSubPayerDto?> _cache;

    public OptumSubPayerQueryHandler(IPlatformConfigApi platformConfigApi, IMemoryCache<OptumSubPayerDto?> cache)
    {
        _platformConfigApi = platformConfigApi ?? throw new ArgumentNullException(nameof(platformConfigApi));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async Task<OptumSubPayerDto?> Handle(OptumSubPayerQuery request, CancellationToken cancellationToken)
    {
        string cacheKey = $"OPTUM_SUBPAY_KEY_{request.OptumSubPayerId}";

        // if the request is for the current date, we'll cache the result
        return await _cache.GetOrCreateAsync(
            cacheKey,
            () => LoadOptumSubPayer(),
            new MemoryCacheEntryOptions().SetAbsoluteExpiration(DateTime.Today.AddDays(1)),
            false).ConfigureAwait(false);

        async Task<OptumSubPayerDto?> LoadOptumSubPayer()
        {
            try
            {
                return await _platformConfigApi.GetOptumSubPayer(request.OptumSubPayerId, cancellationToken).ConfigureAwait(false);
            }
            catch (ApiException apiEx) when (apiEx.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }
    }
}
