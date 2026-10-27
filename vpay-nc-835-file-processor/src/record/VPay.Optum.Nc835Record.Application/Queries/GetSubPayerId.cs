using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using VPay.Extensions.Caching.Memory;
using VPay.Optum.Http.ConfigApi;
using VPay.Optum.Http.ConfigApi.Models;

namespace VPay.Optum.Nc835Record.Application.Queries
{
    public static class GetSubPayerId
    {
        public record Query(string ClientCode, string BillingEntityCode) : IRequest<int?>;

        public class Handler : IRequestHandler<Query, int?>
        {
            private readonly IConfigApi _configApi;
            private readonly IMemoryCache<BillingEntity?> _cache;

            public Handler(
                IConfigApi configApi,
                IMemoryCache<BillingEntity?> cache)
            {
                _configApi = configApi;
                _cache = cache;
            }

            public async Task<int?> Handle(Query request, CancellationToken cancellationToken)
            {
                var memoryOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(DateTime.Today.AddDays(1));

                var value = await _cache.GetOrCreateAsync(GetKey(request), () => LoadBillingEntity(request, cancellationToken), memoryOptions, false).ConfigureAwait(false);

                return value?.Id;
            }

            private async Task<BillingEntity?> LoadBillingEntity(Query request, CancellationToken cancellationToken)
            {
                // if the setting to use the api is true, pull Billing Entity information from the API
                return await _configApi.GetBillingEntity(request.ClientCode, request.BillingEntityCode, cancellationToken).ConfigureAwait(false);
            }

            private static string GetKey(Query query)
            {
                return $"BE_{query.ClientCode.ToUpper()}_{query.BillingEntityCode.ToUpper()}";
            }
        }
    }
}
