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
    public class InventoryDriverFileController : ControllerBase
    {
        private readonly IInventoryDriverFileService _inventoryDriverFileService;

        public InventoryDriverFileController(IInventoryDriverFileService inventoryDriverFileService)
        {
            this._inventoryDriverFileService = inventoryDriverFileService;
        }

        /// <summary>
        /// For a given inventory driver file, this will read the file, staple and also stamp the documents given
        /// to the corresponding transaction document
        /// </summary>
        /// <param name="value">Full path to the inventory driver file (e.g. *.inv.drv)</param>
        /// <returns>No return value</returns>
        [HttpPost]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ParseAndQueueFile([FromBody]string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return new BadRequestResult();
            }

            try
            {
                var result = await _inventoryDriverFileService.ParseFile(value);

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
        /// For a given inventory driver file, this will read the file, staple and also stamp the documents given
        /// to the corresponding transaction document
        /// </summary>
        /// <param name="value">Full path to the inventory driver file (e.g. *.inv.drv)</param>
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

                await _inventoryDriverFileService.QueueFileForParsing(value);
            }
            catch (IOException ex)
            {
                return NotFound(new { ex.Message, ex.StackTrace });
            }

            return Ok(new { isSuccessful = true });
        }
    }
}
