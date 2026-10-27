using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace VPay.DocSys.Generation.Api.Controllers
{
    [ApiVersion("1.0")]
    [ApiVersion("2.0")]
    [Produces("application/json")]
    [Route("api/About", Order = 1)]
    [Route("api/v{version:apiVersion}/About", Order = 2)]
    [ApiController]
    public class AboutController : ControllerBase
    {
        /// <summary>
        /// V1, V2 About the build that created the service
        /// </summary>
        /// <returns>Information about the build that created the service</returns>
        /// <response code="200">Information about the build that created the service</response>
        [HttpGet]
        [MapToApiVersion("1.0")]
        [MapToApiVersion("2.0")]
        [ProducesResponseType(typeof(AboutInfo), 200)]
        public ActionResult<AboutInfo> GetDetailV2()
        {
            return new AboutInfo(this.HttpContext.RequestedApiVersion?.ToString());
        }
    }
}
