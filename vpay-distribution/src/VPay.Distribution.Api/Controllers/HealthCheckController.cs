using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using VPay.DocSys.Common;

namespace VPay.Distribution.Api.Controllers
{
    [ApiVersionNeutral]
    [Produces("application/json")]
    [Route("api/Health")]
    [ApiController]
    public class HealthCheckController : ControllerBase
    {
        private readonly HealthCheckService _healthCheckService;

        public HealthCheckController(HealthCheckService healthCheckService)
        {
            _healthCheckService = healthCheckService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(HealthCheckStatus), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(HealthCheckStatus), StatusCodes.Status503ServiceUnavailable)]
        public async Task<ActionResult<HealthCheckStatus>> CheckHealth()
        {
            var healthReport = await _healthCheckService.CheckHealthAsync();

            var responseStatusCode = healthReport.Status == HealthStatus.Healthy ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable;

            return StatusCode(responseStatusCode, healthReport.ToHealthCheckStatus());
        }
    }
}
