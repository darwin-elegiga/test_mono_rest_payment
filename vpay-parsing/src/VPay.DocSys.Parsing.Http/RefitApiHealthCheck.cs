using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace VPay.DocSys.Parsing.Http
{
    public class RefitApiHealthCheck<TApiInterface> : IHealthCheck
        where TApiInterface : class
    {
        private readonly TApiInterface _api;
        private readonly Func<TApiInterface, Task<HealthStatus>> _ensureHealth;

        public RefitApiHealthCheck(TApiInterface api, Func<TApiInterface, Task<HealthStatus>> ensureHealth)
        {
            _api = api;
            _ensureHealth = ensureHealth;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                return new HealthCheckResult(await _ensureHealth(_api));
            }
            catch (Exception e)
            {
                return new HealthCheckResult(HealthStatus.Unhealthy, exception: e);
            }
        }
    }
}
