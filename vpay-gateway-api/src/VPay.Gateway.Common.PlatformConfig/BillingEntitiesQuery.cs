using System.Net;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Refit;
using VPay.Extensions.Caching.Memory;

namespace VPay.Gateway.Common.PlatformConfig;

public record BillingEntitiesQuery(string ClientCode) : IRequest<List<BillingEntityDto>>;

public class BillingEntitiesQueryHandler : IRequestHandler<BillingEntitiesQuery, List<BillingEntityDto>>
{
    private readonly IPlatformConfigApi _platformConfigApi;
    private readonly IMemoryCache<List<BillingEntityDto>> _cache;

    public BillingEntitiesQueryHandler(IPlatformConfigApi platformConfigApi, IMemoryCache<List<BillingEntityDto>> cache)
    {
        _platformConfigApi = platformConfigApi ?? throw new ArgumentNullException(nameof(platformConfigApi));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async Task<List<BillingEntityDto>> Handle(BillingEntitiesQuery request, CancellationToken cancellationToken)
    {
        string cacheKey = $"BILLING_ENTITY_CLIENT_KEY_{request.ClientCode}";
        const int PageSize = 500;

        return await _cache.GetOrCreateAsync(
            cacheKey,
            () => LoadPages(),
            new MemoryCacheEntryOptions().SetAbsoluteExpiration(DateTime.Today.AddMinutes(30)),
            false).ConfigureAwait(false);

        async Task<List<BillingEntityDto>> LoadPages()
        {
            try
            {
                var page = await _platformConfigApi
                    .GetBillingEntities(request.ClientCode, page: 1, pageSize: PageSize, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                var list = page?.Items ?? new List<BillingEntityDto>();

                if (page == null || page.TotalPages < 2)
                {
                    return list;
                }

                for (var pageNumber = 2; pageNumber <= page.TotalPages; pageNumber++)
                {
                    var pagination = await _platformConfigApi
                        .GetBillingEntities(request.ClientCode, page: pageNumber, pageSize: 500, cancellationToken: cancellationToken)
                        .ConfigureAwait(false);

                    if (pagination != null)
                    {
                        list.AddRange(pagination.Items);
                    }
                }

                return list;
            }
            catch (ApiException apiEx) when (apiEx.StatusCode == HttpStatusCode.NotFound)
            {
                return new List<BillingEntityDto>();
            }
        }
    }
}
