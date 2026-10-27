using Microsoft.AspNetCore.Mvc;
using VPay.Distribution.Api;

namespace VPay.DocSys.Distribution.Api.Controllers
{

    [Produces("application/json")]
    [Route("api/About")]
    [ApiController]
    public class AboutController : ControllerBase
    {

        public AboutController(AboutInfo info)
        {
            _info = info;
        }

        private readonly AboutInfo _info;

        /// <summary>
        /// About the build that created the service
        /// </summary>
        /// <returns>Information about the build that created the service</returns>
        /// <response code="200">Information about the build that created the service</response>
        [HttpGet]
        [ProducesResponseType(typeof(AboutInfo), 200)]
        public AboutInfo GetDetail()
        {
            return _info;
        }

    }

}
