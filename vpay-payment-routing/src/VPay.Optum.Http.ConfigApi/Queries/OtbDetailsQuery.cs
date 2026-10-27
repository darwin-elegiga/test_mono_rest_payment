using System;
using System.Threading.Tasks;
using MediatR;
using VPay.Optum.Http.ConfigApi.Models;
using VPay.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.Memory;
using System.Threading;
using Refit;
using System.Net;

namespace VPay.Optum.Http.ConfigApi.Queries
{
    public class OtbBillingEntityDetailQuery : IRequest<OtbBillingEntityDetailDto>
    {
        public OtbBillingEntityDetailQuery(string clientCode, string fundingAccount)
        {
            ClientCode = clientCode ?? string.Empty;
            FundingAccount = fundingAccount ?? string.Empty;
        }

        public string ClientCode { get; }

        public string FundingAccount { get; }
    }

    public class OtbBillingEntityDetailQueryHandler : IRequestHandler<OtbBillingEntityDetailQuery, OtbBillingEntityDetailDto>
    {
        private readonly IConfigApi _configApi;
        private readonly IMemoryCache<OtbBillingEntityDetailDto> _cache;

        public OtbBillingEntityDetailQueryHandler(IConfigApi configApi, IMemoryCache<OtbBillingEntityDetailDto> cache)
        {
            _configApi = configApi ?? throw new ArgumentNullException(nameof(configApi));
            _cache = cache;
        }

        public async Task<OtbBillingEntityDetailDto> Handle(OtbBillingEntityDetailQuery request, CancellationToken cancellationToken)
        {
            string cacheKey = $"OPTUM_OTB_KEY_{request.ClientCode}_{request.FundingAccount}";

            // if the request is for the current date, we'll cache the result
            return await _cache.GetOrCreateAsync(
                cacheKey,
                () => LoadConfiguration(),
                new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(DateTime.Today.AddDays(1)),
            false).ConfigureAwait(false);

            async Task<OtbBillingEntityDetailDto> LoadConfiguration()
            {
                try
                {
                    return await _configApi.GetOtbBillingEntityDetails(
                        request.ClientCode,
                        request.FundingAccount,
                        cancellationToken
                    ).ConfigureAwait(false);
                }
                catch (ApiException apiEx) when (apiEx.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }
            }
        }
    }
}
