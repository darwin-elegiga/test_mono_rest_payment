using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using VPay.DocSys.Generation.Common;

namespace VPay.DocSys.Generation.Api.Controllers
{

    [ApiVersionNeutral]
    [Produces("application/json")]
    [Route("api/[Controller]")]
    [ApiController]
    public class DropToPrintController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IDropToPrintService _dropToPrintService;
        public DropToPrintController(IDropToPrintService dropToPrintService, ILogger<DropToPrintController> logger)
        {
            _dropToPrintService = dropToPrintService;
            _logger = logger;
        }

        /// <summary>
        /// Used for drop to print status files that bypass generation
        /// </summary>
        /// <param name="statusFilePath">Full path to an STS file that starts with PF</param>
        /// <returns>Exit status code from python call</returns>
        [HttpPost]
        public async Task<IActionResult> DropToPrint(string statusFilePath)
        {
            var result = await _dropToPrintService.CallDropToPrintPython(statusFilePath);
            return Ok(new {PythonStatusCode = result});
        }


    }
}
