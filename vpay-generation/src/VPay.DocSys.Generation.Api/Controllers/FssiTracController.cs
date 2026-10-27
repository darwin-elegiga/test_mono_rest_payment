using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using VPay.DocSys.Storage;

namespace VPay.DocSys.Generation.Api.Controllers
{
    [ApiVersionNeutral]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class FssiTracController : ControllerBase
    {
        private readonly IFssiTracManager _fssiTracManager;

        public FssiTracController(IFssiTracManager fssiTracManager)
        {
            _fssiTracManager = fssiTracManager;
        }

        /// <summary>
        /// Given a string in the form {0}_{1}_{2}_{3}.zip, determines if the given transaction has
        /// already been sent to fssi.
        /// Where:
        /// {0} = StatusRecord.PRTVENDOR
        /// {1} = StatusRecord.TPA
        /// {2} = InputFile
        /// {3} = StatusRecord.PAYTYPE
        /// </summary>
        /// <returns>True if the package has already been sent to FSSI.</returns>
        [HttpPost("SentToFssi")]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<IActionResult> PostSentToFssi([FromBody] string fssiPackageName)
        {
            var alreadySent = await _fssiTracManager.SentToFssi(fssiPackageName);
            return Ok(alreadySent);
        }
    }
}
