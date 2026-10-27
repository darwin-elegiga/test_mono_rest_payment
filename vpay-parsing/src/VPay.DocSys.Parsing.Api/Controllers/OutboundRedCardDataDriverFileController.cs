using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VPay.DocSys.Common;
using VPay.DocSys.Parsing.Common.Interfaces;

namespace VPay.DocSys.Parsing.Api.Controllers
{
    [Route("/api/[controller]")]
    [Produces("application/json")]
    [ApiController]
    public class OutboundRedCardDataDriverFileController : ControllerBase
    {
        private readonly IOutboundRedCardDataDriverFileService _outboundRedCardDataDriverFileService;

        public OutboundRedCardDataDriverFileController(IOutboundRedCardDataDriverFileService outboundRedCardDataDriverFileService)
        {
            this._outboundRedCardDataDriverFileService = outboundRedCardDataDriverFileService;
        }

        /// <summary>
        /// For a given outbound redcard data file, this will read the file and add the relevant entries to the tar file
        /// </summary>
        /// <param name="value">Full path to the outbound redcard data file (e.g. *.outbound_rc_data.drv)</param>
        /// <returns>No return value</returns>
        [HttpPost]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ProcessFile([FromBody]string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return new BadRequestResult();
            }

            try
            {
                var result = await _outboundRedCardDataDriverFileService.ParseFile(value);

                if(result.ResultCode != RedCardProcessResultCode.Success)
                {
                    var resultCode = result.ResultCode.ToString();
                    var description = result.Description;

                    return NotFound(new { Result = resultCode, Reason = description });
                }

                return Ok(new { isSuccessful = true });
            }
            catch (IOException ex)
            {
                return NotFound(new { ex.Message, ex.StackTrace });
            }
        }

        /// <summary>
        /// For a given outbound redcard data file, this will read the file and individually queue the transactions
        /// to a parsing queue
        /// </summary>
        /// <param name="value">Full path to the outbound redcard data file (e.g. *.outbound_rc_data.drv)</param>
        /// <returns>No return value</returns>
        [HttpPost("QueueFileForParsing")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> QueueFileForParsing([FromBody]string value)
        {
            try
            {
                if (string.IsNullOrEmpty(value))
                {
                    return new BadRequestResult();
                }

                await _outboundRedCardDataDriverFileService.QueueFileForParsing(value);
            }
            catch (IOException ex)
            {
                return NotFound(new { ex.Message, ex.StackTrace });
            }

            return Ok(new { isSuccessful = true });
        }
    }
}
