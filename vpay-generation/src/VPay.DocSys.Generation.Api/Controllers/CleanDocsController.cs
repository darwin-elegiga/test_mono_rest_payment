using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Models;
using VPay.DocSys.Storage;

namespace VPay.DocSys.Generation.Api.Controllers
{
    [ApiVersionNeutral]
    [ApiController]
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class CleanDocsController : ControllerBase
    {
        private readonly CleanDocsService _service;

        public CleanDocsController(CleanDocsService service)
        {
            _service = service;
        }

        /// <summary>
        /// Enter the data for an STS file to "clean doc"
        /// </summary>
        /// <param name="statusFilePath">Full path to STS file</param>
        /// <param name="csvTransactionIds">Empty string for all transactions, otherwise, a comma separated string, listing each transactionId you want to delete, with no spaces. MUST match regex /^[0-9,]+$/</param>
        /// <returns>Object listing all the archive_file_ids and the paths to delete.</returns>
        [HttpPost("CleanDocsRequest")]
        [ProducesResponseType(typeof(CleanDocsRequestSummary), 200)]
        public async Task<IActionResult> CleanDocsRequest(string statusFilePath, string csvTransactionIds)
        {
            var result = await _service.CleanDocsRequestInsert(statusFilePath.Trim(), csvTransactionIds?.Trim());
            var summary = new CleanDocsRequestSummary(result);
            return Ok(summary);
        }

        /// <summary>
        /// Verfiy this is really what you want to do.
        /// </summary>
        /// <param name="requestId">Request Id returned from the previous step</param>
        /// <param name="archivesToDelete">Number of archive records to delete.</param>
        /// <param name="deleteDistribution">Do you also want to delete distribution data?</param>
        /// <param name="removeAuxFile">Remove the generated aux file if it exists (RC only)</param>
        /// <param name="removeClaimCheckFiles">Remove the generated document transaction message json files</param>
        /// <returns></returns>
        [HttpPost("CleanDocsVerify")]
        [ProducesResponseType(typeof(CleanDocsServiceResponse), 200)]
        public async Task<IActionResult> CleanDocsVerify(int requestId, int archivesToDelete, bool deleteDistribution, bool removeAuxFile, bool removeClaimCheckFiles)
        {
            var origRequest = await _service.CleanDocsRequestGet(requestId);
            var summary = new CleanDocsRequestSummary(origRequest);
            if (archivesToDelete != summary.ArchivesToDelete)
            {
                return BadRequest(new { Message = "The number of archives to delete entered does not match the expected value." });
            }

            var result = await _service.CleanDocsRequestDelete(origRequest, deleteDistribution, removeAuxFile, removeClaimCheckFiles);
            return Ok(result);
        }

        /// <summary>
        /// Do the actual deletes on the filesystem and the databases.
        /// </summary>
        /// <param name="request">The object with the Stuff to clean</param>
        /// <returns>How many files and archives were deleted.</returns>
        [HttpPost("CleanDocsDelete")]
        [ProducesResponseType(typeof(CleanDocsServiceResponse), 200)]
        [Authorize(Policy = "AllowAll")]
        public async Task<IActionResult> CleanDocsDelete([FromBody] CleanDocsDeleteRequest request)
        {
            var result = await _service.CleanDocsRequestDelete(request.RequestedDeletions, request.DeleteDistribution, request.RemoveAuxFile, true);
            return Ok(result);
        }

        /// <summary>
        /// Retrieve the data for a requested "clean doc".
        /// </summary>
        /// <param name="requestId">A previously requested cleandocs operation</param>
        /// <returns>Object listing all the archive_file_ids and the paths to delete.</returns>
        [HttpPost("CleanDocsRequestResults")]
        [ProducesResponseType(typeof(IList<CleanDocsRequestResult>), 200)]
        public async Task<IActionResult> CleanDocsGetRequestResults(int requestId)
        {
            var result = await _service.CleanDocsRequestGet(requestId);
            return Ok(result);
        }

        /// <summary>
        /// Inserts the request and cleans it in one step, making for much easier "staging" cleanup.
        /// Will NOT work on production.
        /// </summary>
        /// <param name="request">The requested item to insert a clean docs request.</param>
        /// <returns>Results of submitting, then cleaning the document.</returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpPost("CleanDocsWithoutConfirmation")]
        [ProducesResponseType(typeof(CleanDocsServiceResponse), 200)]
        public async Task<IActionResult> CleanDocsWithoutConfirmation([FromBody] CleanDocsInsertRequest request)
        {
            if (request.ApiKey != "verify")
            {
                return BadRequest("NO!");
            }

            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "PROD";
            if (env.ToUpper().StartsWith("PROD"))
            {
                return BadRequest(new { Message = "This method is not available on this system." });
            }

            var resultOfInsert = await _service.CleanDocsRequestInsert(request.StatusFilePath, request.CsvTransactionIds);
            foreach (var cleanDocsRequestResult in resultOfInsert)
            {
                if (String.IsNullOrEmpty(cleanDocsRequestResult.StatusFilePath))
                {
                    cleanDocsRequestResult.StatusFilePath = request.StatusFilePath;
                }
            }
            var resultOfDelete = await _service.CleanDocsRequestDelete(resultOfInsert, true, true, true);
            return Ok(resultOfDelete);
        }

        /// <summary>
        /// Expunges the archive, wiping the record from the database and the filesystem.
        /// </summary>
        /// <param name="request">Requested archive to delete</param>
        /// <returns>Bool indicating success or failure.</returns>
        [HttpPost("ExpungeArchive")]
        public async Task<IActionResult> PostExpungeArchive([FromBody] ExpungeArchiveRequest request)
        {
            var result = await _service.ExpungeArchive(request.TransactionGuid, request.ArchiveId);
            return Ok(result);
        }
    }
}
