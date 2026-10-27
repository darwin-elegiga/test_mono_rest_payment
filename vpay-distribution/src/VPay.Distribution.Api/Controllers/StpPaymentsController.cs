using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using VPay.Distribution.Common;
using VPay.Distribution.Http.Abstractions;
using VPay.Distribution.Http.Abstractions.Dto;
using VPay.DocSys.Data;
using VPay.DocSys.Storage;

namespace VPay.Distribution.Api.Controllers
{
    [Route("/api/[controller]")]
    [Produces("application/json")]
    [ApiController]
    public class StpPaymentsController : ControllerBase
    {
        private readonly IStpPaymentService _stpPaymentService;
        private readonly IRemittanceService _remittanceService;
        private readonly IStorageService _storageService;
        private readonly IPlatformConfigurationApi _platformConfigurationApi;
        private readonly IPaymentRoutingApi _paymentRoutingApi;

        public StpPaymentsController(
            IStpPaymentService stpPaymentService,
            IRemittanceService remittanceService,
            IStorageService storageService,
            IPlatformConfigurationApi platformConfigurationApi,
            IPaymentRoutingApi paymentRoutingApi)
        {
            _stpPaymentService = stpPaymentService ?? throw new ArgumentNullException(nameof(stpPaymentService));
            _remittanceService = remittanceService ?? throw new ArgumentNullException(nameof(remittanceService));
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _platformConfigurationApi = platformConfigurationApi ?? throw new ArgumentNullException(nameof(platformConfigurationApi));
            _paymentRoutingApi = paymentRoutingApi ?? throw new ArgumentNullException(nameof(paymentRoutingApi));
        }

        /// <summary>
        /// Get STP Payment record for a given transactionId
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

        /// <summary>
        /// Reset the STP Payment Remit sent retry count to zero so it can be retried again.
        /// </summary>
        /// <param name="transactionId">The transaction Id.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The STP payment was successfully updated; Not Found if not found; No Content if found but not updated.</returns>
        /// <response code="200">If a record matching the parameters was updated.</response>
        /// <response code="204">If no records were updated.</response>
        /// <response code="400">If the parameters are invalid.</response>
        /// <response code="404">If no record matching the parameters can be found.</response>
        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpPut("/api/[controller]/reset-stp-retries/{transactionId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<StpPaymentDto>> ResetStpPaymentSentRetries(
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
            if (tran.PaymentSentRetries == 0)
            {
                return NoContent();
            }

            var result = await _stpPaymentService
                .ResetStpRemitSentRetries(transactionId, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (result == null)
            {
                return NoContent();
            }

            var dto = StpPaymentDto.Create(result);
            return Ok(dto);
        }

        /// <summary>
        /// Mark the STP Payment as having successfully sent out its pmb message.
        /// </summary>
        /// <param name="transactionId">The transaction Id.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The STP payment was successfully updated; Not Found if not found; No Content if found but not updated.</returns>
        /// <response code="200">If a record matching the parameters was updated.</response>
        /// <response code="204">If no records were updated.</response>
        /// <response code="400">If the parameters are invalid.</response>
        /// <response code="404">If no record matching the parameters can be found.</response>
        [HttpPut("/api/[controller]/mark-pmb-message-sent/{transactionId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<StpPaymentDto>> MarkStpPaymentPmbMessageSent(
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
                .MarkPmbMessageSent(transactionId, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (result == null)
            {
                return NoContent();
            }

            var dto = StpPaymentDto.Create(result);
            return Ok(dto);
        }

        /// <summary>
        /// Mark the STP Payment as having an error while trying to send out its pmb message.
        /// </summary>
        /// <param name="transactionId">The transaction Id.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The STP payment was successfully updated; Not Found if not found; No Content if found but not updated.</returns>
        /// <response code="200">If a record matching the parameters was updated.</response>
        /// <response code="204">If no records were updated.</response>
        /// <response code="400">If the parameters are invalid.</response>
        /// <response code="404">If no record matching the parameters can be found.</response>
        [HttpPut("/api/[controller]/mark-pmb-message-not-sent/{transactionId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<StpPaymentDto>> MarkStpPaymentPmbMessageNotSent(
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
                .MarkPmbMessageNotSent(transactionId, timestamp: null, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (result == null)
            {
                return NoContent();
            }

            var dto = StpPaymentDto.Create(result);
            return Ok(dto);
        }

        /// <summary>
        /// Verify if the SDS 835 document for a given transaction id has been created and it is available for download.
        /// </summary>
        /// <param name="transactionId">The transactionId of the SDS 835 document.</param>
        /// <param name="cancellationToken">The token to cancel the request.</param>
        /// <returns>OK with the provided transaction Id if available; Not Found otherwise</returns>
        /// <response code="200">If the SDS 835 can be downloaded.</response>
        /// <response code="400">If the parameters are invalid.</response>
        /// <response code="404">If the SDS 835 cannot be found.</response>
        [HttpGet("/api/[controller]/find-sds-835/{transactionId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<TransactionExistsDto>> CanDownloadSds835(
            long transactionId,
            CancellationToken cancellationToken = default)
        {
            if (transactionId <= 0)
            {
                return BadRequest("The transaction Id must be greater than zero.");
            }

            try
            {
                var result = await _storageService
                .CanDownloadEdi835(transactionId, cancellationToken)
                .ConfigureAwait(false);

                return Ok(new TransactionExistsDto { TransactionId = transactionId, Exists = result });
            }
            catch (Exception)
            {
                return NotFound();
            }
        }

        /// <summary>
        /// Verify if the PRS 835 document for a given transaction id has been created and it is available for download.
        /// </summary>
        /// <param name="transactionId">The transactionId of the PRS 835 document.</param>
        /// <param name="cancellationToken">The token to cancel the request.</param>
        /// <returns>OK with the provided transaction Id if available; Not Found otherwise</returns>
        /// <response code="200">If the PRS 835 can be downloaded.</response>
        /// <response code="400">If the parameters are invalid.</response>
        /// <response code="404">If the PRS 835 cannot be found.</response>
        /// <response code="500">If the underlying service is not available.</response>
        [HttpGet("/api/[controller]/find-prs-835/{transactionId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<TransactionExistsDto>> CanDownloadPrs835(
            long transactionId,
            CancellationToken cancellationToken = default)
        {
            if (transactionId <= 0)
            {
                return BadRequest("The transaction Id must be greater than zero.");
            }

            Outbound835ExistsDto result;
            try
            {
                result = await _paymentRoutingApi
                    .CanDownloadOutbound835Document(transactionId, cancellationToken)
                    .ConfigureAwait(false);

                return Ok(new TransactionExistsDto { TransactionId = result.TransactionId, Exists = result.Exists });
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
        /// Retrieve all archive document records for the provided transactionId.
        /// </summary>
        /// <param name="transactionId">Internal (VPay) Transaction Id</param>
        /// <returns>A collection of information about archive files.</returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet("/api/[controller]/find-archives/{transactionId}")]
        [ProducesResponseType(typeof(IEnumerable<IArchiveFile>), 200)]
        public async Task<ActionResult<IEnumerable<IArchiveFile>>> GetArchiveFiles(long transactionId)
        {
            var result = await _storageService
                .GetArchiveFiles(transactionId)
                .ConfigureAwait(false);

            return Ok(result);
        }

        /// <summary>
        /// Get the remittance JSON contents for the given transaction Id.
        /// </summary>
        /// <param name="transactionId">The transaction Id.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The remit JSON contents for the given transaction Id.</returns>
        /// <response code="200">If the Remit JSON can be downloaded.</response>
        /// <response code="400">If the parameters are invalid.</response>
        /// <response code="404">If the Remit JSON cannot be found.</response>
        [HttpGet("/api/[controller]/find-remit-json/{transactionId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<string>> GetRemittanceJson(
            [Required] long transactionId,
            CancellationToken cancellationToken = default)
        {
            if (transactionId <= 0)
            {
                return BadRequest("The transaction Id must be greater than zero.");
            }

            var result = await _storageService
                .GetRemitJson(transactionId, cancellationToken)
                .ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(result))
            {
                return NotFound();
            }

            return Ok(result);
        }

        /// <summary>
        /// Get the network partner details for the given network name.
        /// </summary>
        /// <param name="networkPartner">The network partner name.</param>
        /// <param name="cancellationToken">The token to cancel the request.</param>
        /// <returns>OK with STP payment matching the provided transaction Id if available; Not Found otherwise</returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet("/api/[controller]/networks/{networkPartner}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<NetworkDto>> GetNetworkPartner(
            string networkPartner,
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
        /// Search STP Payment records that are pending to send to Remittance Network Partner API
        /// and publish the corresponding PMB messages.
        /// </summary>
        /// <param name="filterDto">The filter criteria to limit to a certain group of payments.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>OK with STP payment list matching the provided search criteria.</returns>
        /// <response code="200">If zero or more records matching the parameters are found.</response>
        /// <response code="400">If the parameters are invalid.</response>
        [HttpGet("/api/[controller]/pending/search")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<IEnumerable<StpPaymentDto>>> GetPendingStpPayments(
            [Required][FromQuery] StpPaymentFilterDto filterDto,
            CancellationToken cancellationToken = default)
        {
            if (filterDto == null)
            {
                return BadRequest("The search criteria is required.");
            }

            var result = await _stpPaymentService.SearchStpPayments(

                networkName: filterDto.NetworkName,
                enrollType: filterDto.EnrollType,
                edi835Type: filterDto.Edi835Type,
                edi835Avail: null,
                remitValid: true,
                paymentSent: false,
                maxResults: filterDto.MaxResults,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            var dto = StpPaymentDto.CreateList(result);
            return Ok(dto);
        }

        /// <summary>
        /// Process one or more STP payments that are pending to send to Remittance Network Partner API
        /// and publish the corresponding PMB messages.
        /// </summary>
        /// <param name="transactionIds"></param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The count of pending transactions found.</returns>
        /// <response code="202">If at least one pending transaction was found.</response>
        /// <response code="204">If no pending transactions were found.</response>
        [HttpPost("/api/[controller]/pending/process")]
        [ProducesResponseType(202)]
        [ProducesResponseType(204)]
        public async Task<ActionResult<int>> ProcessPendingToSend(
            [Required] long[] transactionIds,
            CancellationToken cancellationToken = default)
        {
            if (transactionIds.Length == 0)
            {
                return NoContent();
            }

            // Find pending payments.
            var pending = await _stpPaymentService.GetStpPayments(
                transactionIds: transactionIds,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (!pending.Any())
            {
                return NoContent();
            }

            // Validate input payments.
            foreach (var payment in pending)
            {
                if (!payment.IsB2B())
                {
                    return BadRequest($"The transaction Id {payment.TransactionId} is not a B2B STP payment.");
                }
                if (payment.PaymentSent)
                {
                    return BadRequest($"The transaction Id {payment.TransactionId} has already been sent.");
                }
                if (!payment.RemitValid)
                {
                    return BadRequest($"The transaction Id {payment.TransactionId} does not have a valid remit document.");
                }
                if (payment.PaymentSentRetries >= 6)
                {
                    return BadRequest($"The transaction Id {payment.TransactionId} has been tried too many times.");
                }
            }
            foreach (var payment in pending)
            {
                await _stpPaymentService
                    .ProcessPendingStpPayment(payment, cancellationToken)
                    .ConfigureAwait(false);
            }
            return Accepted(pending.Count());
        }

        /// <summary>
        /// Start processing STP payments that are pending to send to Remittance Network Partner API
        /// and publish the corresponding PMB messages.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The count of pending transactions found.</returns>
        /// <response code="202">If at least one pending transaction was found.</response>
        /// <response code="204">If no pending transactions were found.</response>
        [HttpPost("/api/[controller]/pending/start")]
        [ProducesResponseType(202)]
        [ProducesResponseType(204)]
        public async Task<ActionResult<int>> StartPendingToSend(
            CancellationToken cancellationToken = default)
        {
            // Find 835s for B2B STP payments that have not been sent.
            var pending = await _stpPaymentService.SearchStpPayments(
                enrollType: EnrollTypes.B2B,  // only process B2B payments for now.
                edi835Avail: null,
                remitValid: true,
                paymentSent: false,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (!pending.Any())
            {
                return NoContent();
            }

            await _stpPaymentService
                .StartPendingToSend(cancellationToken)
                .ConfigureAwait(false);

            return Accepted(pending.Count);
        }

        /// <summary>
        /// Publish a PMB message tailored for STP payment for the provided transaction Id.
        /// </summary>
        /// <param name="transactionId">The transaction Id.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The count of messages sent.</returns>
        /// <response code="202">If a message matching the parameters was published.</response>
        /// <response code="204">If no messages were published.</response>
        /// <response code="400">If the parameters are invalid.</response>
        /// <response code="404">If no message matching the parameters can be found.</response>
        [HttpPost("/api/[controller]/publish-pmb-message/{transactionId}")]
        [ProducesResponseType(202)]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<bool>> PublishPmbMessage(
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
            if (!tran.IsB2B())
            {
                return BadRequest("The transaction is not a B2B STP payment.");
            }
            if (tran.PaymentSent)
            {
                return NoContent();
            }

            var result = await _stpPaymentService
                .PublishPmbMessage(transactionId, cancellationToken)
                .ConfigureAwait(false);

            return Accepted(result);
        }

        /// <summary>
        /// Validate the contents of the remit document via the Remittance Partner Network API.
        /// </summary>
        /// <param name="transactionId">The transaction Id.</param>
        /// <param name="enrollmentType">The enrollment type: B2B or C2B.</param>
        /// <param name="remitDocument">The remit document to validate.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>True if remit document contents are valid; False otherwise.</returns>
        [HttpPost("/api/[controller]/validate-remittance/{transactionId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<TransactionValidDto>> ValidateRemitDocument(
            [Required] long transactionId,
            [Required] string enrollmentType,
            StpB2BRemittanceDocument remitDocument,
            CancellationToken cancellationToken = default)
        {
            if (transactionId <= 0)
            {
                return BadRequest("The transaction Id must be greater than zero.");
            }
            if (string.IsNullOrWhiteSpace(enrollmentType))
            {
                return BadRequest("The enrollment type must be provided.");
            }
            if (!EnrollTypes.IsB2B(enrollmentType) && !EnrollTypes.IsC2B(enrollmentType))
            {
                return BadRequest("The enrollment type must be B2B or C2B.");
            }
            if (remitDocument == null)
            {
                return BadRequest("Remit document is required.");
            }

            var valid = await _remittanceService
                .ValidateRemitDocument(transactionId, enrollmentType, remitDocument.ToJsonString(), cancellationToken)
                .ConfigureAwait(false);

            var result = new TransactionValidDto
            {
                TransactionId = transactionId,
                IsValid = valid
            };
            return Ok(result);
        }

        /// <summary>
        /// Post the contents of the remit document via the Remittance Partner Network API.
        /// </summary>
        /// <param name="transactionId">The transaction Id.</param>
        /// <param name="enrollmentType">The enrollment type: B2B or C2B.</param>
        /// <param name="remitDocument">The remit document to validate.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>True if remit document contents are valid; False otherwise.</returns>
        [HttpPost("/api/[controller]/send-remittance/{transactionId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<TransactionValidDto>> SendRemitDocument(
            [Required] long transactionId,
            [Required] string enrollmentType,
            StpB2BRemittanceDocument remitDocument,
            CancellationToken cancellationToken = default)
        {
            if (transactionId <= 0)
            {
                return BadRequest("The transaction Id must be greater than zero.");
            }
            if (string.IsNullOrWhiteSpace(enrollmentType))
            {
                return BadRequest("The enrollment type must be provided.");
            }

            if (!Enum.TryParse(enrollmentType, out RemittanceNetworkEnrollmentTypes _))
            {
                return BadRequest("The enrollment type must be B2B or C2B.");
            }
            if (remitDocument == null)
            {
                return BadRequest("Remit document is required.");
            }

            var valid = await _remittanceService
                .PostRemitDocument(transactionId, enrollmentType, remitDocument.ToJsonString(), cancellationToken)
                .ConfigureAwait(false);

            var result = new TransactionValidDto
            {
                TransactionId = transactionId,
                IsValid = valid
            };
            return Ok(result);
        }
    }
}

