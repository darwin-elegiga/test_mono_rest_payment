using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VPay.DocSys.Common;
using VPay.DocSys.Parsing.Common;

namespace VPay.DocSys.Parsing.Api.Controllers
{
    [Route("/api/[controller]")]
    [Produces("application/json")]
    [ApiController]
    public class Inbound835FileController : ControllerBase
    {
        private readonly IInbound835FileService _inbound835FileService;

        public Inbound835FileController(IInbound835FileService inbound835FileService)
        {
            _inbound835FileService = inbound835FileService;
        }

        /// <summary>
        /// Queues the specified inbound 835 file for processing.
        /// </summary>
        /// <param name="filePath">The full path and file name of the inbound 835 file to be queued.</param>
        /// <returns></returns>
        [HttpPost("QueueFileForProcessing")]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> QueueFileForProcessing([FromBody] string filePath)
        {
            ArgumentHelpers.ThrowIf.IsNullOrEmpty(filePath, nameof(filePath));

            await _inbound835FileService.QueueFileForProcessing(filePath);

            return Accepted();
        }

        /// <summary>
        /// Processes the specified inbound 835 file.
        /// </summary>
        /// <param name="filePath">The full path and file name of the inbound 835 file to be queued.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        [HttpPost("ProcessFile")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ProcessFile([FromBody] string filePath, CancellationToken cancellationToken)
        {
            ArgumentHelpers.ThrowIf.IsNullOrEmpty(filePath, nameof(filePath));

            await _inbound835FileService.ProcessFile(filePath, cancellationToken);

            return Ok();
        }
    }
}
