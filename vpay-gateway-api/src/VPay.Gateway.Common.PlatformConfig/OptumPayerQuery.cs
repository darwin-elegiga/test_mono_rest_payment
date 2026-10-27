using System.Net;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Refit;
using VPay.Extensions.Caching.Memory;

namespace VPay.Gateway.Common.PlatformConfig;

public record OptumPayerQuery(string OptumPayerId) : IRequest<List<OptumSubPayerDto>>;

public class OptumPayerQueryHandler : IRequestHandler<OptumPayerQuery, List<OptumSubPayerDto>>
{
    private readonly IPlatformConfigApi _platformConfigApi;
    private readonly IMemoryCache<List<OptumSubPayerDto>> _cache;

    public OptumPayerQueryHandler(IPlatformConfigApi platformConfigApi, IMemoryCache<List<OptumSubPayerDto>> cache)
    {
        _platformConfigApi = platformConfigApi ?? throw new ArgumentNullException(nameof(platformConfigApi));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async Task<List<OptumSubPayerDto>> Handle(OptumPayerQuery request, CancellationToken cancellationToken)
    {
        string cacheKey = $"OPTUM_PAY_KEY_{request.OptumPayerId}";
        const int PageSize = 500;

        return await _cache.GetOrCreateAsync(
            cacheKey,
            () => LoadPages(),
            new MemoryCacheEntryOptions().SetAbsoluteExpiration(DateTime.Today.AddMinutes(30)),
            false).ConfigureAwait(false);

        async Task<List<OptumSubPayerDto>> LoadPages()
        {
            try
            {
                var page = await _platformConfigApi
                    .GetOptumPayer(request.OptumPayerId, page: 1, pageSize: PageSize, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                var list = page?.Items ?? new List<OptumSubPayerDto>();

                if (page == null || page.TotalPages < 2)
                {
                    return list;
                }

                for (var pageNumber = 2; pageNumber <= page.TotalPages; pageNumber++)
                {
                    var pagination = await _platformConfigApi
                        .GetOptumPayer(request.OptumPayerId, page: pageNumber, pageSize: 500, cancellationToken: cancellationToken)
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
                return new List<OptumSubPayerDto>();
            }
        }
    }
}
