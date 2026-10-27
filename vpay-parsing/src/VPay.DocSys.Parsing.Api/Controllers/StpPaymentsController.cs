using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using VPay.DocSys.Parsing.Common;
using VPay.DocSys.Parsing.Common.Interfaces;
using VPay.DocSys.Parsing.Http.Abstractions.PlatformConfiguration;
using VPay.DocSys.Parsing.Http.Abstractions.PlatformConfiguration.Dto;

namespace VPay.DocSys.Parsing.Api.Controllers
{
    [Route("/api/[controller]")]
    [Produces("application/json")]
    [ApiController]
    public class StpPaymentsController : ControllerBase
    {
        private readonly IStpPaymentService _stpPaymentService;
        private readonly IPlatformConfigurationApi _platformConfigurationApi;

        public StpPaymentsController(
            IStpPaymentService stpPaymentService,
            IPlatformConfigurationApi platformConfigurationApi)
        {
            _stpPaymentService = stpPaymentService;
            _platformConfigurationApi = platformConfigurationApi;
        }

        /// <summary>
        /// Get STP Payment record for the provided transactionId.
        /// </summary>
        /// <param name="transactionId">The transaction Id.</param>
        /// <param name="useRepository">The flag to choose to use DocSys Data Repository or Status File Tracker.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>OK with the STP payment matching the provided transaction Id if available; Not Found otherwise</returns>
        /// <response code="200">If one or more records matching the parameters are found.</response>
        /// <response code="400">If the parameters are invalid.</response>
        /// <response code="404">If no records matching the parameters can be found.</response>
        [HttpGet("/api/[controller]/{transactionId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<StpPaymentDto>> GetStpPayment(
            [Required]long transactionId,
            bool useRepository = false,
            CancellationToken cancellationToken = default)
        {
            if (transactionId <= 0)
            {
                return BadRequest("The transaction Id must be greater than zero.");
            }

            var result = await _stpPaymentService
                .GetStpPayment(transactionId, useRepository, cancellationToken)
                .ConfigureAwait(false);
            if (result == null)
            {
                return NotFound();
            }
            var dto = StpPaymentDto.Create(result);
            return Ok(dto);
        }

        /// <summary>
        /// Get list of STP Payment records for the provided list of transactionIds.
        /// </summary>
        /// <param name="transactionIds">The list of transaction Ids.</param>
        /// <param name="useRepository">The flag to choose to use DocSys Data Repository or Status File Tracker.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>OK with the list of STP payments matching the provided transaction Ids if available; Not Found otherwise</returns>
        /// <response code="200">If one or more records matching the parameters are found.</response>
        /// <response code="400">If the parameters are invalid.</response>
        /// <response code="404">If no records matching the parameters can be found.</response>
        [HttpGet("/api/[controller]/list")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<IEnumerable<StpPaymentDto>>> GetStpPayments(
            [FromQuery] List<long> transactionIds,
            bool useRepository = false,
            CancellationToken cancellationToken = default)
        {
            if (transactionIds == null || transactionIds.Count == 0)
            {
                return BadRequest("The transaction Id list must not be empty.");
            }
            if (transactionIds.Any(id => id <= 0))
            {
                return BadRequest("All transaction Ids must be greater than zero.");
            }

            var result = await _stpPaymentService
                .GetStpPayments(transactionIds, useRepository, cancellationToken)
                .ConfigureAwait(false);

            result = result.ToList();

            if (!result.Any())
            {
                return NotFound();
            }
            var dto = StpPaymentDto.CreateList(result);
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
        /// Insert STP Payment records for the provided STP payment group.
        /// </summary>
        /// <param name="group">The group of payments to insert.</param>
        /// <param name="cancellationToken">The token to cancel the request.</param>
        /// <returns></returns>
        /// <response code="200">If zero or more records matching the parameters are inserted.</response>
        /// <response code="400">If the parameters are invalid.</response>
        /// <response code="500">If bulk insert operation fails to insert the records into the table.</response>
        [HttpPost("/api/[controller]")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<StpPaymentCreatedGroupDto>> InsertStpPayments(
            [FromBody] StpPaymentGroupDto group,
            CancellationToken cancellationToken = default)
        {
            if (group == null)
            {
                return BadRequest("The group criteria is required.");
            }

            var validation = group.Validate();
            if (validation.IsValid == false)
            {
                var errors = validation.Errors.Select(e => e.ErrorMessage);
                return BadRequest("Validation failed: " + string.Join("; ", errors));
            }

            try
            {
                var network = await _platformConfigurationApi
                    .GetNetworkPartner(group.NetworkName, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                if (network == null)
                {
                    return BadRequest("The network partner does not exist or is not valid.");
                }
            }
            catch (Refit.ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return BadRequest("The network partner does not exist or is not valid.");
            }
            catch (Refit.ApiException ex) when (ex.StatusCode != System.Net.HttpStatusCode.OK)
            {
                return StatusCode(500, $"Platform Config API Error ({ex.StatusCode}) : {ex.Message}");
            }

            var count = await _stpPaymentService.InsertStpPayments(
                group.TransactionIds,
                group.NetworkName,
                group.EnrollType,
                group.Edi835Type,
                group.RemitValid,
                group.Edi835Avail,
                group.PaymentSent,
                group.PmbMessageSent,
                cancellationToken);

            var dto = new StpPaymentCreatedGroupDto
            {
                NetworkName = group.NetworkName,
                EnrollType = group.EnrollType,
                Edi835Type = group.Edi835Type,
                PaymentCount = count,
                IsSuccessful = true,
            };
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
            [Required] long transactionId,
            CancellationToken cancellationToken = default)
        {
            if (transactionId <= 0)
            {
                return BadRequest("The transaction Id must be greater than zero.");
            }

            var tran = await _stpPaymentService
                .GetStpPayment(transactionId, cancellationToken: cancellationToken)
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
            [Required] long transactionId,
            CancellationToken cancellationToken = default)
        {
            if (transactionId <= 0)
            {
                return BadRequest("The transaction Id must be greater than zero.");
            }

            var tran = await _stpPaymentService
                .GetStpPayment(transactionId, cancellationToken: cancellationToken)
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
            [Required] long transactionId,
            CancellationToken cancellationToken = default)
        {
            if (transactionId <= 0)
            {
                return BadRequest("The transaction Id must be greater than zero.");
            }

            var tran = await _stpPaymentService
                .GetStpPayment(transactionId, cancellationToken: cancellationToken)
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
            [Required] long transactionId,
            CancellationToken cancellationToken = default)
        {
            if (transactionId <= 0)
            {
                return BadRequest("The transaction Id must be greater than zero.");
            }

            var tran = await _stpPaymentService
                .GetStpPayment(transactionId, cancellationToken: cancellationToken)
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

        /// <summary>
        /// Get the network partner details for the given network name.
        /// </summary>
        /// <param name="networkPartner">The network partner name.</param>
        /// <param name="cancellationToken">The token to cancel the request.</param>
        /// <returns>OK with STP payment matching the provided transaction Id if available; Not Found otherwise</returns>
        [HttpGet("/api/[controller]/networks/{networkPartner}")]
        public async Task<ActionResult<NetworkDto>> GetNetworkPartner(
            [Required][MaxLength(50)] string networkPartner,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(networkPartner))
            {
                return BadRequest("The network partner must be provided.");
            }

            NetworkDto result;
            try
            {
                result = await _platformConfigurationApi
                    .GetNetworkPartner(networkPartner, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                if (result == null)
                {
                    return NotFound();
                }
                return Ok(result);
            }
            catch (Refit.ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound();
            }
            catch (Refit.ApiException ex) when (ex.StatusCode != System.Net.HttpStatusCode.OK)
            {
                return StatusCode(500, $"Platform Config API Error ({ex.StatusCode}) : {ex.Message}");
            }
        }

        /// <summary>
        /// Get the PRS enabled flag for the given origin, client, and billing entity.
        /// </summary>
        /// <param name="originCode">The origin code.</param>
        /// <param name="clientCode">The client code.</param>
        /// <param name="billingEntityCode">The billing entity code.</param>
        /// <param name="cancellationToken">The token to cancel the request.</param>
        /// <returns>OK with STP payment matching the provided transaction Id if available; Not Found otherwise</returns>
        [HttpGet("/api/[controller]/prs-config/origins/{originCode}/clients/{clientCode}")]
        public async Task<ActionResult<OptumPrsConfigurationDto>> GetPrsConfiguration(
            [Required][MaxLength(10)] string originCode,
            [Required][MaxLength(10)] string clientCode,
            [FromQuery][MaxLength(20)] string billingEntityCode,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(originCode))
            {
                return BadRequest("The origin code must be provided.");
            }
            if (string.IsNullOrWhiteSpace(clientCode))
            {
                return BadRequest("The client code must be provided.");
            }

            // Determine if this is a PRS account
            OptumPrsConfigurationDto result;
            try
            {
                result = await _platformConfigurationApi
                    .GetOptumPrsConfiguration(clientCode, originCode, billingEntityCode, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                if (result == null)
                {
                    return NotFound();
                }

                PrsConfigurationDto dto = new()
                {
                    OriginCode = originCode,
                    ClientCode = clientCode,
                    BillingEntityCode = billingEntityCode ?? string.Empty,
                    IsPrsEnabled = result.IsPrsEnabled
                };                
                return Ok(dto);
            }
            catch (Refit.ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound();
            }
            catch (Refit.ApiException ex) when (ex.StatusCode != System.Net.HttpStatusCode.OK)
            {
                return StatusCode(500, $"Platform Config API Error ({ex.StatusCode}) : {ex.Message}");
            }
        }
    }
}
