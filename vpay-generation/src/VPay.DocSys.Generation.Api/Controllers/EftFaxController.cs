using System;
using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Common.Helpers;
using VPay.DocSys.Storage;

namespace VPay.DocSys.Generation.Api.Controllers
{
    [ApiVersionNeutral]
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public class EftFaxController : ControllerBase
    {
        private readonly IFaxMessageService _faxMessageService;
        private readonly IFaxMessagePayloadProcessor _faxMessagePayloadProcessor;
        private readonly ITiffMessageQueuer _tiffMessageQueuer;
        private readonly ILogger _logger;

        public EftFaxController(
            IFaxMessageService faxMessageService,
            IFaxMessagePayloadProcessor faxMessagePayloadProcessor,
            ITiffMessageQueuer tiffMessageQueuer,
            ILogger<EftFaxController> logger)
        {
            ArgumentHelpers.ThrowIf.IsNull(faxMessageService, nameof(faxMessageService));
            ArgumentHelpers.ThrowIf.IsNull(faxMessagePayloadProcessor, nameof(faxMessagePayloadProcessor));
            ArgumentHelpers.ThrowIf.IsNull(tiffMessageQueuer, nameof(tiffMessageQueuer));
            ArgumentHelpers.ThrowIf.IsNull(logger, nameof(logger));

            _faxMessageService = faxMessageService;
            _faxMessagePayloadProcessor = faxMessagePayloadProcessor;
            _tiffMessageQueuer = tiffMessageQueuer;
            _logger = logger;
        }

        /// <summary>
        /// Given a fax xml file, will return the file path for the pdf
        /// Only for Eft faxes xml files
        /// </summary>
        /// <param name="xmlFilePath">xml file path</param>
        /// <returns>pdf file created in the same directory as <paramref name="xmlFilePath"/></returns>
        [HttpPost("Create")]
        [ProducesResponseType(typeof(string), StatusCodes.Status201Created)]
        public async Task<IActionResult> CreatePdfFile([FromBody] string xmlFilePath)
        {
            try
            {
                ArgumentHelpers.ThrowIf.IsNullOrWhiteSpace(xmlFilePath, nameof(xmlFilePath));

                var result = await _faxMessageService.CreateEftFaxPdfFromXmlFile(xmlFilePath);

                return Created(Request.GetEncodedPathAndQuery(), result);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not create pdf from {File}", xmlFilePath.Sanitize());
                return BadRequest(e);
            }
        }

        /// <summary>
        /// Given a fax xml string, will return the <see cref="IArchiveFile"/> information
        /// Only for Eft faxes xml files
        /// </summary>
        /// <param name="xmlFileContent">xml file content as escaped string</param>
        /// <returns>archive that was created</returns>
        [HttpPost("CreateAndArchive")]
        [ProducesResponseType(typeof(IArchiveFile), StatusCodes.Status201Created)]
        public async Task<IActionResult> CreateAndArchive([FromBody]string xmlFileContent)
        {
            try
            {
                ArgumentHelpers.ThrowIf.IsNullOrEmpty(xmlFileContent, nameof(xmlFileContent));

                var result = await _faxMessageService.CreateArchiveForEftFaxXmlString(xmlFileContent);

                return Created(Request.GetEncodedPathAndQuery(), result);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not create pdf from xml string");
                return BadRequest(e);
            }
        }

        [HttpPost("QueueFax")]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        public IActionResult QueueFax([FromBody]FaxMessagePayloadRequest request)
        {
            try
            {
                _faxMessagePayloadProcessor.ProcessPayload(request);

                return Accepted(request);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not create fax queue message from payload");
                return BadRequest(e);
            }
        }

        [HttpPost("QueueTiff")]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        public IActionResult QueueTiff([FromBody] TiffQueueMessage request)
        {
            try
            {
                _tiffMessageQueuer.QueueTiffMessage(request);

                return Accepted(request);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not create tiff queue message from request");
                return BadRequest(e);
            }
        }
    }
}
