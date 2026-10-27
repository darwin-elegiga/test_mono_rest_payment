using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VPay.DocSys.Data;
using VPay.DocSys.Parsing.Common.Interfaces;

namespace VPay.DocSys.Parsing.Api.Controllers
{
    [Route("/api/[controller]")]
    [Produces("application/json")]
    [ApiController]
    public class SupportController : ControllerBase
    {
        private readonly IStatusFileTracker _statusFileTracker;
        private readonly IDriverFileService _driverFileService;
        private readonly IStatusFileService _statusFileService;
        private readonly IRedCardRepository _redCardRepository;
        private readonly IArchiveRecordService _archiveRecordService;

        public SupportController(
            IStatusFileTracker statusFileTracker,
            IDriverFileService driverFileService,
            IStatusFileService statusFileService,
            IRedCardRepository redCardRepository,
            IArchiveRecordService archiveRecordService)
        {
            _statusFileTracker = statusFileTracker;
            _driverFileService = driverFileService;
            _statusFileService = statusFileService;
            _redCardRepository = redCardRepository;
            _archiveRecordService = archiveRecordService;
        }

        /// <summary>
        /// Returns the details about the specified <paramref name="statusFile"/>.
        /// </summary>
        /// <param name="statusFile">The full path to the status file.</param>
        /// <returns>An object that contains the details about the specified <paramref name="statusFile"/>.</returns>
        [HttpGet("StatusFile/{statusFile}/Summary")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(IDictionary<string, object>), StatusCodes.Status200OK)]
        public IActionResult GetStatusFileSummary(string statusFile)
        {
            if (string.IsNullOrEmpty(statusFile))
            {
                return new BadRequestResult();
            }

            try
            {
                return Ok(_statusFileTracker.GetStatusFileSummary(Uri.UnescapeDataString(statusFile)));
            }
            catch (FileNotFoundException)
            {
                return NotFound("No record found for the specified file.");
            }
        }

        /// <summary>
        /// For a given status file, this will regenerate the aux file
        /// </summary>
        /// <param name="statusFile">The full path to the sts file.</param>
        /// <returns>No return value</returns>
        [HttpPost("StatusFile/RegenerateAuxFile")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> RegenerateAuxFile([FromBody] string statusFile)


        {
            if (string.IsNullOrEmpty(statusFile))
            {
                return BadRequest();
            }

            try
            {
                await _statusFileService.RegenerateAuxFile(statusFile);
                return Ok();
            }
            catch (FileNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (ArgumentNullException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (ArgumentException)
            {
                return BadRequest("statusFile must be a .sts file");
            }
            catch (NotSupportedException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException)
            {
                return BadRequest("Possible Configuration Issue");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        /// <summary>
        /// Returns a value indicating whether or not the .lock file associated with the specified <paramref name="driverFile"/> exists.
        /// </summary>
        /// <param name="driverFile">The full path to the driver file.</param>
        /// <returns>
        ///   <c>true</c> if the .lock file exists; otherwise, <c>false</c>.
        /// </returns>
        [HttpGet("DriverFile/{driverFile}/IsLocked")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public IActionResult GetIsDriverFileLocked(string driverFile)
        {
            if (string.IsNullOrEmpty(driverFile))
            {
                return new BadRequestResult();
            }

            try
            {
                return Ok(_driverFileService.IsLocked(Uri.UnescapeDataString(driverFile)));
            }
            catch (FileNotFoundException)
            {
                return NotFound("The specified driver file does not exist.");
            }
        }

        /// <summary>
        /// Deletes the .lock file associated with the specified <paramref name="driverFile"/>.
        /// </summary>
        /// <param name="driverFile">The full path to the driver file.</param>
        /// <returns><c>true</c> if the .lock file does not exists or is successfully deleted; otherwise, <c>false</c>.</returns>
        [HttpPost("DriverFile/{driverFile}/DeleteLockFile")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [Authorize(Policy = "AllowAll")]
        public IActionResult DeleteDriverFileLockFile(string driverFile)
        {
            if (string.IsNullOrEmpty(driverFile))
            {
                return new BadRequestResult();
            }

            try
            {
                return Ok(_driverFileService.ReleaseLock(Uri.UnescapeDataString(driverFile)));
            }
            catch (FileNotFoundException)
            {
                return NotFound("The specified driver file does not exist.");
            }
        }

        /// <summary>
        /// Deletes the .log file associated with the specified <paramref name="driverFile"/>.
        /// </summary>
        /// <param name="driverFile">The full path to the driver file.</param>
        /// <returns><c>true</c> if the .log file does not exists or is successfully deleted; otherwise, <c>false</c>.</returns>
        [HttpPost("DriverFile/{driverFile}/DeleteLogFile")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [Authorize(Policy = "AllowAll")]
        public IActionResult DeleteDriverFileLogFile(string driverFile)
        {
            if (string.IsNullOrEmpty(driverFile))
            {
                return new BadRequestResult();
            }

            try
            {
                return Ok(_driverFileService.DeleteLogFile(Uri.UnescapeDataString(driverFile)));
            }
            catch (FileNotFoundException)
            {
                return NotFound("The specified driver file does not exist.");
            }
        }

        /// <summary>
        /// For a given status file, this will generate and publish a message to the Distribution Queue
        /// </summary>
        /// <param name="statusFilePath">Full path to STS</param>
        /// <param name="distType">M, F or S for Fax, Mail or SendBack; or blank for all.</param>
        /// <returns>No return value</returns>
        [HttpPost("RequeueDistributionMessage")]
        public async Task<IActionResult> RequeueDistributionMessage(string statusFilePath, string distType)
        {
            if (string.IsNullOrEmpty(statusFilePath))
            {
                return new BadRequestResult();
            }

            var distMsg = await _statusFileService.GenerateAndPublishDistributionMessage(statusFilePath, distType);

            return new ContentResult() { Content = distMsg, ContentType = "application/json" };
        }

        /// <summary>
        /// For a given TransactionId, gets the current/max RedCardTransactionStatus
        /// </summary>
        /// <param name="transactionId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpGet("RedCardTransactionStatus/{transactionId}/")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRedCardTransactionStatus([FromRoute] long transactionId, CancellationToken cancellationToken)
        {
            var currentStatusCode = await _redCardRepository.GetTransactionStatus(transactionId, cancellationToken);

            return Ok(new
            {
                CurrentStatus = currentStatusCode,
                CurrentStatusCode = (int)currentStatusCode,
            });
        }

        /// <summary>
        /// For a given TransactionId and RedCardTransactionStatusCode, inserts a new RedCardTransactionStatus if the StatusCode is greater than 0.
        /// </summary>
        /// <param name="transactionId"></param>
        /// <param name="statusCode"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpPut("RedCardTransactionStatus/{transactionId}/{statusCode}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PutRedCardTransactionStatus([FromRoute] long transactionId, [FromRoute] RedCardTransactionStatusCode statusCode, CancellationToken cancellationToken)
        {
            if (statusCode == 0 || !Enum.IsDefined(typeof(RedCardTransactionStatusCode), statusCode))
            {
                return BadRequest(new
                {
                    Error = $"Cannot insert {(int)statusCode} ({statusCode}), delete statuses instead"
                });
            }

            var executionResult = await _redCardRepository.UpdateTransactionStatus(transactionId, statusCode, cancellationToken);

            return Ok(new
            {
                RowsInserted = executionResult,
            });
        }

        /// <summary>
        /// For a given TransactionId and RedCardTransactionStatusCode, removes said status if it exists and if no higher status exists in the database.
        /// </summary>
        /// <param name="transactionId"></param>
        /// <param name="statusCode"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpDelete("RedCardTransactionStatus/{transactionId}/{statusCode}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Authorize(Policy = "AllowAll")]
        public async Task<IActionResult> DeleteRedCardTransactionStatus([FromRoute] long transactionId, [FromRoute] RedCardTransactionStatusCode statusCode, CancellationToken cancellationToken)
        {
            var currentStatusCode = await _redCardRepository.GetTransactionStatus(transactionId, cancellationToken);

            if (currentStatusCode > statusCode)
            {
                return BadRequest(new
                {
                    Error = $"Current status is {(int)currentStatusCode} ({currentStatusCode}), cannot delete status of {(int)statusCode} ({statusCode})"
                });
            }

            var executionResult = await _redCardRepository.ClearTransactionStatus(transactionId, statusCode, cancellationToken);

            return Ok(new
            {
                RowsDeleted = executionResult,
            });
        }

        /// <summary>
        /// Calls the following stored procedure with the supplied <paramref name="statusFileId"/>
        /// [archive].[usp_Restore_ByPaymentStatusFileId]
        /// </summary>
        /// <param name="statusFileId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpPost("RestoreArchiveRecordsByStatusFileId")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RestoreArchiveRecordsByStatusFileId(int statusFileId, CancellationToken cancellationToken)
        {
            await _archiveRecordService.RestoreByStatusFileId(statusFileId, cancellationToken).ConfigureAwait(false);

            return Ok();
        }

        /// <summary>
        /// Calls the following stored procedure with the supplied <paramref name="statusFileId"/>
        /// [archive].[usp_TruncateAndArchive_ByPaymentStatusFileId]
        /// </summary>
        /// <param name="statusFileId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpPost("ArchiveRecordsByStatusFileId")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ArchiveRecordsByStatusFileId(int statusFileId, CancellationToken cancellationToken)
        {
            await _archiveRecordService.ArchiveByStatusFileId(statusFileId, cancellationToken).ConfigureAwait(false);

            return Ok();
        }
    }
}

