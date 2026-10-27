using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using VPay.DocSys.Parsing.Common.Extensions;
using VPay.DocSys.Parsing.Common.Interfaces;

namespace VPay.DocSys.Parsing.Api.Controllers
{
    [Route("/api/[controller]")]
    [Produces("application/json")]
    [ApiController]
    public class StatusFileController : ControllerBase
    {
        private readonly IStatusFileService _statusFileService;
        private readonly IStatusFileTracker _statusFileTracker;

        public StatusFileController(IStatusFileService statusFileService, IStatusFileTracker statusFileTracker)
        {
            _statusFileService = statusFileService;
            _statusFileTracker = statusFileTracker;
        }

        /// <summary>
        /// For a given status file, this will read the status and relevant check, tar, driver and config files and individually queue the transactions
        /// to a DocumentTransaction queue
        /// </summary>
        /// <param name="value">Full path to the status file</param>
        /// <returns>No return value</returns>
        [HttpPost]
        public async Task<IActionResult> ParseAndQueueFile([FromBody] string value)
        {
            try
            {
                if (string.IsNullOrEmpty(value))
                {
                    return new BadRequestResult();
                }

                await _statusFileService.ParseAndQueueFile(value);
            }
            catch (FileNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message, StackTrace = ex.StackTrace });
            }
            catch (DirectoryNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message, StackTrace = ex.StackTrace });
            }

            return Ok(new { isSuccessful = true });
        }

        /// <summary>
        /// For a given status file, this will read the status and relevant check, tar, driver and config files and individually queue the only transactions
        /// listed in csvTransactionIds
        /// </summary>
        /// <param name="stsFilePath">Full path to the status file</param>
        /// <param name="csvTransactionIds">Comma separated list of transactionIds.  All elements must parse to Int64.</param>
        /// <returns>No return value</returns>
        [HttpPost("RegenTransactions")]
        public async Task<IActionResult> ParseAndQueueTransactions([FromBody] string stsFilePath, string csvTransactionIds)
        {
            try
            {
                if (string.IsNullOrEmpty(stsFilePath) && string.IsNullOrEmpty(csvTransactionIds))
                {
                    return new BadRequestResult();
                }

                List<long> transactionIds = csvTransactionIds.Split(',').Select(long.Parse).ToList();



                await _statusFileService.ParseAndQueueTransactions(stsFilePath, transactionIds, true);
            }


            catch (InvalidForRegenerationException ex)
            {
                return BadRequest(new { ex.Message, ex.StackTrace });
            }
            catch (FormatException ex)
            {
                return BadRequest(new { ex.Message, ex.StackTrace });
            }
            catch (OverflowException ex)
            {
                return BadRequest(new { ex.Message, ex.StackTrace });
            }
            catch (FileNotFoundException ex)
            {
                return NotFound(new { ex.Message, ex.StackTrace });
            }
            catch (DirectoryNotFoundException ex)
            {
                return NotFound(new { ex.Message, ex.StackTrace });
            }
            catch (Exception ex)
            {
                return BadRequest(new { ex.Message, ex.StackTrace });
            }
            return Ok(new { isSuccessful = true });
        }

        /// <summary>
        /// Will take the given STS file and place it on the StatusFileParsing queue for processing.
        /// </summary>
        /// <param name="value">Full path to the status file</param>
        /// <returns>No return value</returns>
        [HttpPost("QueueFileForParsing")]
        public async Task<IActionResult> QueueFileForParsing([FromBody] string value)
        {
            try
            {
                if (string.IsNullOrEmpty(value))
                {
                    return new BadRequestResult();
                }

                await _statusFileService.QueueFileForParsing(value, null, false, value.IsEmergencyFile());
            }
            catch (FileNotFoundException ex)
            {
                return NotFound(new { ex.Message, ex.StackTrace });
            }
            catch (DirectoryNotFoundException ex)
            {
                return NotFound(new { ex.Message, ex.StackTrace });
            }

            return Ok(new { isSuccessful = true });
        }

        /// <summary>
        /// For a given status file, this will check the document completion times of document archives for the transactions in the file
        /// </summary>
        /// <param name="fileName">File name of the status file (without the full path)</param>
        /// <returns>No return value</returns>
        [HttpGet]
        [Route("/api/[controller]/DocumentStatus/{fileName}")]
        public IActionResult DocumentStatus(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return new BadRequestResult();
            }
            var docResult = _statusFileTracker.GetDocumentStatus(fileName);
            return new JsonResult(docResult);

        }

        /// <summary>
        /// For a given status file, this will check the document completion times of document archives for the transactions in the file
        /// </summary>
        /// <param name="statusFilePath">Full path to STS</param>
        /// <param name="distType">M, F or S for Fax, Mail or SendBack; or blank for all.</param>
        /// <returns>No return value</returns>
        [HttpPost("DistributionMessage")]
        public async Task<IActionResult> DistributionMessage(string statusFilePath, string distType)
        {
            if (string.IsNullOrEmpty(statusFilePath))
            {
                return new BadRequestResult();
            }

            var distMsg = await _statusFileService.GenerateDistributionMessage(statusFilePath, distType);

            return new ContentResult() { Content = distMsg, ContentType = "application/json" };
        }
    }
}

