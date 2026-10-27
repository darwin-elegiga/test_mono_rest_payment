using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using VPay.DocSys.Generation.Common.Interfaces;
using VPay.DocSys.Generation.Common.Models;

namespace VPay.DocSys.Generation.Api.Controllers
{
    // TODO: Does this controller need [ApiVersionNeutral] ?
    [Route("/api/[controller]")]
    [Produces("application/json")]
    [ApiController]
    //[NonController]
    public class StpPaymentsController : ControllerBase
    {
        private readonly IStpPaymentService _stpPaymentService;

        public StpPaymentsController(IStpPaymentService stpPaymentService)
        {
            _stpPaymentService = stpPaymentService;
        }

        /// <summary>
        /// Get STP Payment record for a given transactionId.
        /// </summary>
        /// <param name="transactionId">The transaction Id.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>OK with STP payment matching the provided transaction Id if available; Not Found otherwise</returns>
        /// <response code="200">If one or more records matching the parameters are found.</response>
        /// <response code="400">If the parameters are invalid.</response>
        /// <response code="404">If no records matching the parameters can be found.</response>
        [HttpGet("/api/[controller]/{transactionId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<StpPaymentDto>> GetStpPayment(
            long transactionId,
            CancellationToken cancellationToken = default)
        {
            if (transactionId <= 0)
            {
                return BadRequest("The transaction Id must be greater than zero.");
            }

            var result = await _stpPaymentService
                .GetStpPayment(transactionId, cancellationToken)
                .ConfigureAwait(false);
            if (result == null)
            {
                return NotFound();
            }
            var dto = StpPaymentDto.Create(result);
            return Ok(dto);
        }

        /// <summary>
        /// Search STP Payment records for the given search criteria.
        /// </summary>
        /// <param name="searchDto">The search criteria.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>OK with STP payment list matching the provided search criteria.</returns>
        /// <response code="200">If zero or more records matching the parameters are found.</response>
        /// <response code="400">If the parameters are invalid.</response>
        [HttpGet("/api/[controller]/search")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<IEnumerable<StpPaymentDto>>> SearchStpPayments(
            [Required][FromQuery] StpPaymentSearchDto searchDto,
            CancellationToken cancellationToken = default)
        {
            if (searchDto == null)
            {
                return BadRequest("The search criteria is required.");
            }

            var validation = searchDto.Validate();
            if (validation.IsValid == false)
            {
                var errors = validation.Errors.Select(e => e.ErrorMessage);
                return BadRequest("Validation failed: " + string.Join("; ", errors));
            }

            var result = await _stpPaymentService.SearchStpPayments(
                searchDto.NetworkName,
                searchDto.EnrollType,
                searchDto.Edi835Type,
                searchDto.Edi835Avail,
                searchDto.RemitValid,
                searchDto.PaymentSent,
                searchDto.PmbMessageSent,
                searchDto.PaymentSentRetries,
                searchDto.MaxResults,
                cancellationToken).ConfigureAwait(false);

            var dto = StpPaymentDto.CreateList(result);
            return Ok(dto);
        }

        /// <summary>
        /// Mark the STP Payment as having a valid Remit content.
        /// </summary>
        /// <param name="transactionId">The transaction Id.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The STP payment was successfully updated; Not Found if not found; No Content if found but not updated.</returns>
        /// <response code="200">If a record matching the parameters was updated.</response>
        /// <response code="204">If no records were updated.</response>
        /// <response code="400">If the parameters are invalid.</response>
        /// <response code="404">If no record matching the parameters can be found.</response>
        [HttpPut("/api/[controller]/mark-remit-valid/{transactionId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<StpPaymentDto>> MarkStpPaymentRemitValid(
            long transactionId,
            CancellationToken cancellationToken = default)
        {
            if (transactionId <= 0)
            {
                return BadRequest("The transaction Id must be greater than zero.");
            }

            var tran = await _stpPaymentService
                .GetStpPayment(transactionId, cancellationToken)
                .ConfigureAwait(false);
            if (tran == null)
            {
                return NotFound();
            }

            var result = await _stpPaymentService
                .MarkRemitValid(transactionId, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (result == null)
            {
                return NoContent();
            }

            var dto = StpPaymentDto.Create(result);
            return Ok(dto);
        }

        /// <summary>
        /// Mark the STP Payment as having an available 835 file.
        /// </summary>
        /// <param name="transactionId">The transaction Id.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The STP payment was successfully updated; Not Found if not found; No Content if found but not updated.</returns>
        /// <response code="200">If a record matching the parameters was updated.</response>
        /// <response code="204">If no records were updated.</response>
        /// <response code="400">If the parameters are invalid.</response>
        /// <response code="404">If no record matching the parameters can be found.</response>
        [HttpPut("/api/[controller]/mark-edi-835-available/{transactionId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<StpPaymentDto>> MarkStpPaymentEdi835Avail(
            long transactionId,
            CancellationToken cancellationToken = default)
        {
            if (transactionId <= 0)
            {
                return BadRequest("The transaction Id must be greater than zero.");
            }

            var tran = await _stpPaymentService
                .GetStpPayment(transactionId, cancellationToken)
                .ConfigureAwait(false);
            if (tran == null)
            {
                return NotFound();
            }

            var result = await _stpPaymentService
                .MarkEdi835Avail(transactionId, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (result == null)
            {
                return NoContent();
            }

            var dto = StpPaymentDto.Create(result);
            return Ok(dto);
        }

        /// <summary>
        /// Mark the STP Payment as having been successfully sent out.
        /// </summary>
        /// <param name="transactionId">The transaction Id.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The STP payment was successfully updated; Not Found if not found; No Content if found but not updated.</returns>
        /// <response code="200">If a record matching the parameters was updated.</response>
        /// <response code="204">If no records were updated.</response>
        /// <response code="400">If the parameters are invalid.</response>
        /// <response code="404">If no record matching the parameters can be found.</response>
        [HttpPut("/api/[controller]/mark-payment-sent/{transactionId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<StpPaymentDto>> MarkStpPaymentPaymentSent(
            long transactionId,
            CancellationToken cancellationToken = default)
        {
            if (transactionId <= 0)
            {
                return BadRequest("The transaction Id must be greater than zero.");
            }

            var tran = await _stpPaymentService
                .GetStpPayment(transactionId, cancellationToken)
                .ConfigureAwait(false);
            if (tran == null)
            {
                return NotFound();
            }

            var result = await _stpPaymentService
                .MarkPaymentSent(transactionId, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (result == null)
            {
                return NoContent();
            }

            var dto = StpPaymentDto.Create(result);
            return Ok(dto);
        }

        /// <summary>
        /// Mark the STP Payment as having an error while trying to be sent out and increases the retry count.
        /// </summary>
        /// <param name="transactionId">The transaction Id.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The STP payment was successfully updated; Not Found if not found; No Content if found but not updated.</returns>
        /// <response code="200">If a record matching the parameters was updated.</response>
        /// <response code="204">If no records were updated.</response>
        /// <response code="400">If the parameters are invalid.</response>
        /// <response code="404">If no record matching the parameters can be found.</response>
        [HttpPut("/api/[controller]/mark-payment-not-sent/{transactionId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<StpPaymentDto>> MarkStpPaymentPaymentNotSent(
            long transactionId,
            CancellationToken cancellationToken = default)
        {
            if (transactionId <= 0)
            {
                return BadRequest("The transaction Id must be greater than zero.");
            }

            var tran = await _stpPaymentService
                .GetStpPayment(transactionId, cancellationToken)
                .ConfigureAwait(false);
            if (tran == null)
            {
                return NotFound();
            }

            var result = await _stpPaymentService
                .MarkPaymentNotSent(transactionId, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (result == null)
            {
                return NoContent();
            }

            var dto = StpPaymentDto.Create(result);
            return Ok(dto);
        }
    }
}
