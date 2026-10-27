using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Refit;
using VPay.Extensions.Caching.Memory;
using VPay.Optum.Http.PreferencingApi.Models;

namespace VPay.Optum.Http.PreferencingApi.Queries
{
    public record OptumAchEnrollmentSearchQuery(OptumAchEnrollmentSearchParam Request) : IRequest<OptumAchEnrollment>;

    public class OptumAchEnrollmentsSearchQueryHandler : IRequestHandler<OptumAchEnrollmentSearchQuery, OptumAchEnrollment>
    {
        private readonly IPreferencingApi _preferencingApi;
        private readonly IMemoryCache<OptumAchEnrollment> _cache;

        public OptumAchEnrollmentsSearchQueryHandler(IPreferencingApi preferencingApi, IMemoryCache<OptumAchEnrollment> cache)
        {
            _preferencingApi = preferencingApi;
            _cache = cache;
        }



        public async Task<OptumAchEnrollment> Handle(OptumAchEnrollmentSearchQuery request, CancellationToken cancellationToken)
        {
            var body = request.Request;

            string cacheKey = $"OptumAchEnrollment_KEY_{body.ClientCode}_{body.TaxId}";

            // Cache the results for 10 minutes
            return await _cache.GetOrCreateAsync(
                cacheKey,
                () => PerformSearch(),
                new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(DateTime.Now.AddMinutes(10)),
            false).ConfigureAwait(false);

            async Task<OptumAchEnrollment> PerformSearch()
            {
                try
                {
                    var result = await _preferencingApi.GetOptumAchPayorEnrollmentsSearch(
                        body,
                        cancellationToken
                    ).ConfigureAwait(false);

                    return new OptumAchEnrollment(body.ClientCode, body.TaxId, result != null);

                }
                catch (ApiException apiEx) when (apiEx.StatusCode == HttpStatusCode.NoContent)
                {
                    return new OptumAchEnrollment(body.ClientCode, body.TaxId, false);
                }
                catch (ApiException apiEx) when (apiEx.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }
            }


        }
    }

}
