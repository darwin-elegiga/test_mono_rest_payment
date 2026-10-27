using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common.Models.Sql;

namespace VPay.DocSys.Generation.Api.Controllers
{
    [ApiController]
    [ApiVersionNeutral]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public class BundledFaxController : ControllerBase
    {
        private readonly IBundledFaxService _bundledFaxService;

        public BundledFaxController(IBundledFaxService bundledFaxService)
        {
            _bundledFaxService = bundledFaxService;
        }

        [HttpPost("SendBundledFax")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<IActionResult> SendBundledFax(BundledFaxParams faxParams)
        {
            var returnMessage = await _bundledFaxService.CreateBundledFax(faxParams);

            return Ok(returnMessage);
        }

        /// <summary>
        /// Re-submit all the failed bundled faxes from on or after a given date
        /// </summary>
        /// <param name="retryFromDate">defaults to today</param>
        /// <param name="bundledFaxId"> defaults to null and will be ignore if defaulted</param>
        /// <returns></returns>
        [HttpPost("RetryFailedBundledFaxes")]
        [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
        public async Task<IActionResult> RetryFailedBundledFaxes(DateTime? retryFromDate, long? bundledFaxId = null)
        {
            DateTime startDate = retryFromDate ?? DateTime.Now;
            var returnMessage = await _bundledFaxService.RetryFailedBundledFaxesAsync(startDate, bundledFaxId);

            return Ok(returnMessage);
        }

        /// <summary>
        /// List bundled faxes that failed on or after a given date
        /// </summary>
        /// <param name="retryFromDate">defaults to today</param>
        /// <returns></returns>
        [HttpPost("GetFailedBundledFaxes")]
        [ProducesResponseType(typeof(List<FailedBundledFax>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> GetFailedBundledFaxes(DateTime? retryFromDate)
        {
            DateTime startDate = retryFromDate ?? DateTime.Now;
            var listOfFaxes = await _bundledFaxService.GetFailedBundledFaxesAsync(startDate);

            if (!listOfFaxes.Any())
            {
                return NoContent();
            }

            return Ok(listOfFaxes);
        }

        [HttpPost("GenerateCorrespondenceId")]
        [ProducesResponseType(typeof(long), StatusCodes.Status200OK)]
        public async Task<IActionResult> GenerateBundledFaxCorrespondenceId(List<long> transactionId, string faxNumber, string documentPath, int pageCount)
        {
            var result = await _bundledFaxService.GenerateBundledFaxCorrespondenceId(transactionId, faxNumber, documentPath, pageCount);

            return Ok(new { result.CorrespondenceId });
        }
    }
}
