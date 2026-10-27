using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EdiFabric.Core.Model.Edi.X12;
using EdiFabric.Templates.Hipaa5010;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VPay.EdiFabric;
using VPay.EdiFabric.Helpers;
using VPay.EdiFabric.Helpers.Models;
using VPay.Optum.Http.ConfigApi.Queries;
using VPay.Optum.Http.ReconReportsApi.Queries;
using VPay.Optum.PaymentRouting.Api.Controllers.Models;
using VPay.Optum.PaymentRouting.Application.InboundPayments.Commands;
using VPay.Optum.PaymentRouting.Application.Outbound.Models;
using VPay.Optum.PaymentRouting.ApProcessor.Abstractions;
using VPay.Optum.PaymentRouting.ApProcessor.Constants;
using VPay.Optum.PaymentRouting.Common.Configuration;
using VPay.Optum.PaymentRouting.Common.Helpers;
using VPay.Optum.PaymentRouting.Common.Serialization;
using VPay.Optum.PaymentRouting.Data.Helpers;
using VPay.Optum.PaymentRouting.Data.SmartEnums;
using VPay.Optum.PaymentRouting.Data.Sql.ApReap.Commands;
using VPay.Optum.PaymentRouting.Data.Sql.ApReap.Queries;
using VPay.Optum.PaymentRouting.Data.Sql.BatchSummary;
using VPay.Optum.PaymentRouting.Data.Sql.InboundBatch.Commands;
using VPay.Optum.PaymentRouting.Data.Sql.InboundBatch.Queries;
using VPay.Optum.PaymentRouting.Data.Sql.InboundHeader;
using VPay.Optum.PaymentRouting.Data.Sql.InboundPayment.Commands;
using VPay.Optum.PaymentRouting.Data.Sql.InboundPayment.Queries;
using VPay.Optum.PaymentRouting.Data.Sql.Models;
using VPay.Optum.PaymentRouting.Data.Sql.Outbound835File.Queries;
using VPay.Optum.PaymentRouting.Data.Sql.OutboundPayment.Queries;
using VPay.Optum.PaymentRouting.OutboundProcessor.Helpers;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace VPay.Optum.PaymentRouting.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Produces(MediaTypeNames.Application.Json)]
    [Consumes(MediaTypeNames.Application.Json)]
    [ExcludeFromCodeCoverage]
    public class SupportController : ControllerBase
    {
        private readonly ILogger<SupportController> _logger;
        private readonly IMediator _mediator;
        private readonly IOptumBus _optumBus;
        private readonly IFileSystem _fileSystem;
        private readonly IUserInfo _userInfo;
        private readonly string _modifiedBy;
        private readonly IApReapVPayToOptumConverter _converter;
        private readonly OptumQueueNameSettings _queueNameSettings;

        private const string DefaultRevEv = "411858498";

        public SupportController(
            ILogger<SupportController> logger,
            IMediator mediator,
            IOptumBus optumBus,
            IFileSystem fileSystem,
            IUserInfo userInfo,
            IApReapVPayToOptumConverter converter,
            OptumQueueNameSettings queueNameSettings)
        {
            _logger = logger;
            _mediator = mediator;
            _optumBus = optumBus;
            _fileSystem = fileSystem;
            _userInfo = userInfo;
            _converter = converter;
            _queueNameSettings = queueNameSettings;
            _modifiedBy = userInfo.GetCurrentUserName();
        }

        /// <summary>
        ///     Queues a payment record or EOF to be added to a batch to start the process of receiving a payment or EOF.
        /// </summary>
        /// <param name="body">Contains the payment record data to be added.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the work.</param>
        /// <response code="200">If a batch status was successfully set to completed.</response>
        [HttpPost("queue-payment-record")]
        [ProducesResponseType(Status200OK)]
        public async Task<ActionResult> QueuePaymentRecord([FromBody] QueuePaymentRecordBody body, CancellationToken cancellationToken)
        {
            await _mediator.Send(new QueuePaymentRecord.Command { PaymentRecord = body.PaymentRecord }, cancellationToken);
            return Ok();
        }

        /// <summary>
        ///     Queues a payment summary for a batch to start the process of receiving a batch.
        /// </summary>
        /// <param name="body">Contains the payment summary data to be queued.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the work.</param>
        /// <response code="200">If the summary was successfully published to the exchange</response>
        [HttpPost("queue-summary")]
        public async Task<Unit> PublishPaymentSummary([FromBody] PaymentSummary body, CancellationToken cancellationToken)
        {
            return await _mediator.Send(new PublishPaymentSummary.Command { PaymentSummary = body }, cancellationToken);
        }

        /// <summary>
        ///     Inserts a payment summary for a batch into the database.
        /// </summary>
        /// <param name="body">Contains the payment summary data to be saved.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the work.</param>
        /// <response code="200">If the summary was successfully published to the exchange</response>
        [HttpPost("save-summary")]
        public async Task<ActionResult<BatchSummary>> SavePaymentSummary([FromBody] PaymentSummary body, CancellationToken cancellationToken)
        {
            var batchSummary = await _mediator.Send<BatchSummary>(new SaveBatchSummary.Command { PaymentSummary = body }, cancellationToken);
            return Ok(batchSummary);
        }

        /// <summary>
        ///     Insert a payment record or EOF into the database.
        /// </summary>
        /// <param name="body">Contains the payment record data to be added.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the work.</param>
        /// <response code="200">If a batch status was successfully set to completed.</response>
        [HttpPost("save-payment-record")]
        [ProducesResponseType(Status200OK)]
        public async Task<ActionResult<BatchSummary>> SavePaymentRecord([FromBody] QueuePaymentRecordBody body, CancellationToken cancellationToken)
        {
            var batchSummary = await _mediator.Send<BatchSummary>(new SavePaymentRecord.Command { PaymentRecord = body.PaymentRecord }, cancellationToken);
            return Ok(batchSummary);
        }

        [HttpPost("check-batch-completion")]
        public async Task<ActionResult> CheckBatchCompletion([FromQuery] long inboundBatchId,
            CancellationToken cancellationToken)
        {
            await _mediator.Send(new CheckBatchCompletion.Command(inboundBatchId), cancellationToken);
            return Ok();
        }

        /// <summary>
        /// Updates the UserKey field on an Inbound Payment
        /// </summary>
        /// <param name="inboundPaymentId">The Id field of the Inbound Payment</param>
        /// <param name="userKey">The current UserKey field of the payment</param>
        /// <param name="jiraIssueId">The support ticket number (i.e.: PAY-1234, DATASUP-1234, or INC-123456 for Service Now) </param>
        /// <param name="cancellationToken">A token that can be used to cancel the work.</param>
        /// <response code="200">If a payment UserKey was successfully set.</response>
        /// <response code="400">If the {<paramref name="userKey"/>}_{<paramref name="jiraIssueId"/>} value is too long.</response>
        /// <response code="404">If the Id and UserKey combination were not found.</response>
        /// <returns><see cref="UpdateInboundPaymentUserKeySql.Result"/> if successful, otherwise exception detail</returns>
        [HttpPost("update-payment-record/{inboundPaymentId}/{userKey}/{jiraIssueId}")]
        [ProducesResponseType(typeof(UpdateInboundPaymentUserKeySql.Result), Status200OK)]
        [ProducesResponseType(typeof(Microsoft.Data.SqlClient.SqlException), Status404NotFound)]
        public async Task<IActionResult> UpdateUserKey(
            [Range(1, long.MaxValue)] long inboundPaymentId,
            [MaxLength(49)] string userKey,
            [RegularExpression(@"^\w{3,}\-\d+$")][MaxLength(50)] string jiraIssueId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _mediator.Send(new UpdateInboundPaymentUserKeySql
                    .Command(inboundPaymentId, userKey, jiraIssueId), cancellationToken));
            }
            catch (Microsoft.Data.SqlClient.SqlException e)
            {
                return e.Message.Contains("not exist", StringComparison.OrdinalIgnoreCase)
                    ? NotFound(e) : BadRequest(e);
            }
        }

        /// <summary>
        /// Updates the Status field to CANCELLED on an Inbound Payment
        /// </summary>
        /// <param name="inboundPaymentId">The Id field of the Inbound Payment</param>
        /// <param name="cancellationToken">A token that can be used to cancel the work.</param>
        /// <response code="200">If a payment Status was successfully set.</response>
        /// <response code="400">Invalid operation for given request</response>
        /// <response code="404">If the Id was not found.</response>
        /// <returns>nothing if successful, otherwise exception detail</returns>
        [HttpPost("update-payment-record/{inboundPaymentId}/cancel")]
        [ProducesResponseType(Status200OK)]
        [ProducesResponseType(typeof(InvalidOperationException), Status403Forbidden)]
        public async Task<IActionResult> CancelPayment(
            [Range(1, long.MaxValue)] long inboundPaymentId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _mediator.Send(new CancelInboundPayment
                    .Command(inboundPaymentId), cancellationToken);

                return Ok();
            }
            catch (InvalidOperationException e)
            {
                return e.Message.Contains("not exist", StringComparison.OrdinalIgnoreCase)
                    ? NotFound(e) : BadRequest(e);
            }
        }

        /// <summary>
        /// Updates the TotalPaymentCount and TotalPaymentAmount fields on an Inbound Batch
        /// </summary>
        /// <param name="inboundBatchId">The Id field of the Inbound Batch</param>
        /// <param name="totalPaymentCount">The total payments in the batch</param>
        /// <param name="totalPaymentAmount">The sum of payment amounts in the batch</param>
        /// <param name="cancellationToken">A token that can be used to cancel the work.</param>
        /// <response code="200">If a batch UserKey was successfully set.</response>
        /// <response code="404">If the Id was not found.</response>
        /// <response code="400">Invalid operation for given request</response>
        /// <returns>nothing if successful, otherwise exception detail</returns>
        [HttpPost("update-batch-record/{inboundBatchId}/{totalPaymentCount}/{totalPaymentAmount}")]
        [ProducesResponseType(Status200OK)]
        [ProducesResponseType(typeof(Microsoft.Data.SqlClient.SqlException), Status404NotFound)]
        public async Task<IActionResult> UpdateBatchTotals(
            [Range(1, long.MaxValue)] long inboundBatchId,
            int totalPaymentCount,
            decimal totalPaymentAmount,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _mediator.Send(new UpdateInboundBatchTotalsSql
                    .Command(inboundBatchId, totalPaymentCount, totalPaymentAmount), cancellationToken);

                return Ok();
            }
            catch (Microsoft.Data.SqlClient.SqlException e)
            {
                return e.Message.Contains("not exist", StringComparison.OrdinalIgnoreCase)
                    ? NotFound(e) : BadRequest(e);
            }
        }

        /// <summary>
        /// Updates the Status field to CANCELLED on an Inbound Batch
        /// </summary>
        /// <param name="inboundBatchId">The Id field of the Inbound Batch</param>
        /// <param name="cancellationToken">A token that can be used to cancel the work.</param>
        /// <response code="200">If a batch Status was successfully set.</response>
        /// <response code="400">Invalid operation for given request</response>
        /// <response code="404">If the Id was not found.</response>
        /// <returns>nothing if successful, otherwise exception detail</returns>
        [HttpPost("update-batch-record/{inboundBatchId}/cancel")]
        [ProducesResponseType(Status200OK)]
        [ProducesResponseType(typeof(Microsoft.Data.SqlClient.SqlException), Status404NotFound)]
        public async Task<IActionResult> CancelBatch(
            [Range(1, long.MaxValue)] long inboundBatchId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _mediator.Send(new CancelInboundBatch
                    .Command
                { InboundBatchId = inboundBatchId }, cancellationToken);

                return Ok();
            }
            catch (InvalidOperationException e)
            {
                return e.Message.Contains("not exist", StringComparison.OrdinalIgnoreCase)
                    ? NotFound(e) : BadRequest(e);
            }
        }

        /// <summary>
        /// Updates the Status field to CANCELLED on an Inbound Batch
        /// </summary>
        /// <param name="inboundBatchId">The Id field of the Inbound Batch</param>
        /// <param name="jiraTicket">The support ticket number (i.e.: PAY-1234, DATASUP-1234, or INC-123456 for Service Now) </param>
        /// <param name="cancellationToken">A token that can be used to cancel the work.</param>
        /// <response code="200">If a batch Status was successfully set.</response>
        /// <response code="400">Invalid operation for given request</response>
        /// <response code="404">If the Id was not found.</response>
        /// <returns>nothing if successful, otherwise exception detail</returns>
        [HttpPost("update-batch-record/{inboundBatchId}/cancel-all/{jiraTicket}")]
        [ProducesResponseType(Status200OK)]
        [ProducesResponseType(Status400BadRequest)]
        [ProducesResponseType(typeof(Microsoft.Data.SqlClient.SqlException), Status404NotFound)]
        public async Task<IActionResult> CancelBatchAll(
            [Range(1, long.MaxValue)] long inboundBatchId,
            [RegularExpression(@"^\w{3,}\-\d+$")][MaxLength(50)] string jiraTicket,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _mediator.Send(new CancelBatchAndPayments.Command(inboundBatchId, jiraTicket), cancellationToken);

                return Ok();
            }
            catch (InvalidOperationException e)
            {
                return e.Message.Contains("record exists for", StringComparison.OrdinalIgnoreCase)
                    ? NotFound(e) : BadRequest(e);
            }
        }

        /// <summary>
        /// Detailed summaries of payments, EOF(Headers) and batch summaries for which we do not have the all the necessary
        /// parts to send the batch to the iSeries for processing.
        /// </summary>
        /// <param name="minutesLag">Do not report on batches until they are this many minutes old. Prevents false positives from
        /// batches that take a few minutes to upload all of the payments.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>JSON objects with relevant details as well as a reason why the query caught the particular case.</returns>
        /// <remarks>We have had issues where Optum has timeouts/500 errors on preferencing, and ends up having to restart the batch
        /// on their side.<br />
        /// This ends up trying to insert the same UserKey with a different OptumBatchId (unique constraint violation).<br />
        /// There are support endpoints to change they names of the UserKeys above (requires a JIRA ticket).<br />
        /// It is also recommended to also "cancel" the batch once you know, so that it won't show up in this report (or the count
        /// endpoint).
        /// </remarks>
        [HttpGet("incompleteBatches/{minutesLag?}")]
        [ProducesResponseType(Status200OK)]
        public async Task<IEnumerable<IncompleteBatchDetails>> GetIncompleteBatchesDetails(
            int? minutesLag = 20,
            CancellationToken cancellationToken = default
        )
        {
            var results = await (_mediator.Send(new GetIncompleteBatchesSql.Query(minutesLag)
                , cancellationToken).ConfigureAwait(false));
            return (results);
        }

        /// <summary>
        /// Count of any payments or batch summaries for which we do not have the all the necessary
        /// parts to send the batch to the iSeries for processing.
        /// </summary>
        /// <param name="minutesLag">Do not report on batches until they are this many minutes old. Prevents false positives from
        /// batches that take a few minutes to upload all of the payments.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>A count of batches that require attention.  Call the incompleteBatches endpoint to get the details to communicate with
        /// Optum and determine why the batch is incomplete.</returns>
        [HttpGet("incompleteBatchesCount")]
        [ProducesResponseType(Status200OK)]
        public async Task<int> GetIncompleteBatchesCount(
            int? minutesLag = 20,
            CancellationToken cancellationToken = default
        )
        {
            var results = await (_mediator.Send(new GetIncompleteBatchesSql.Query(minutesLag)
                , cancellationToken).ConfigureAwait(false));
            return (results?.Count() ?? 0);
        }

        /// <summary>
        /// Re-sends to the Optum Azure queue an AP/REAP message converted by the AP processor, bypassing
        /// the routing rules and skipping all checks for Status, Payment Type, and Transaction ID uniqueness.
        /// </summary>
        /// <param name="request">The raw AP/REAP Azure message coming form iSeries after conversion
        /// as it is logged in Graylog.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>200 if successful; 400 if error.</returns>
        [HttpPost("SendApReapMessage")]
        [ProducesResponseType(typeof(ApReapPayload), Status200OK)]
        public async Task<IActionResult> SendApReapMessage(
            [FromBody][Required] ApReapPayload request,
            CancellationToken token = default)
        {
            // Create AP/REAP accounts payable message directly from payload.
            VpayApReap message = request.ToAvro();

            // Log contents of message
            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["UserKey"] = request.UnconsolPaymentNumber.SanitizeForLogging(256),
                ["MessageType"] = request.PaymentType.SanitizeForLogging(256),
                ["TransactionId"] = 0,
                ["ReapTransactionId"] = 0
            });
            var convertedMessageJson = JsonConvert.SerializeObject(message);
            var sanitizedConvertedMessageJson = StringHelper.Sanitize(convertedMessageJson);
            _logger.LogInformation("Converted Message: {ConvertedMessage} (endpoint:SendApReapMessage)", sanitizedConvertedMessageJson);

            var endpoint = await _optumBus.GetSendEndpoint(new Uri($"queue:{_queueNameSettings.AccountPayable}"));

            // Send message to the Azure encoded bus.
            await endpoint.Send(message, token);

            return Ok(request);
        }

        /// <summary>
        /// Re-sends to the Optum Azure queue an AP/REAP message as originally stored on the iSeries, bypassing
        /// the routing rules and skipping all checks for Status, Payment Type, and Transaction ID uniqueness.
        /// </summary>
        /// <param name="request">The AP/REAP Rabbit message coming from iSeries before conversion
        /// as it is stored on the iSeries.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>200 if successful; 400 if error.</returns>
        [HttpPost("SendApReapMessageFromISeries")]
        [ProducesResponseType(typeof(ApReapPayload), Status200OK)]
        public async Task<IActionResult> SendApReapMessageFromISeries(
            [FromBody][Required] ApReapISeriesPayload request,
            CancellationToken token = default)
        {
            // Create AP/REAP accounts payable message by converting an AP/REAP Rabbit message coming from iSeries.
            VpayApReap message = await _converter.ConvertApReapMessage(request.ToRabbit());

            // Log contents of message
            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["UserKey"] = request.UserKey.SanitizeForLogging(256),
                ["MessageType"] = request.MessageType.SanitizeForLogging(256),
                ["TransactionId"] = request.TransactionId.ToString().SanitizeForLogging(256),
                ["ReapTransactionId"] = request.ReapTransactionId.ToString().SanitizeForLogging(256)
            });
            var sanitizedMessage = StringHelper.SanitizeObjectStrings(message);
            _logger.LogInformation("Converted Message: {ConvertedMessage} (endpoint:SendApReapMessageFromISeries)", JsonConvert.SerializeObject(sanitizedMessage));

            var endpoint = await _optumBus.GetSendEndpoint(new Uri($"queue:{_queueNameSettings.AccountPayable}"));

            // Send message to the Azure encoded bus.
            await endpoint.Send(message, token);

            var convertedRequest = ApReapPayload.Create(message);
            return Ok(convertedRequest);
        }

        /// <summary>
        /// Re-sends to the Optum Azure queue a list of AP/REAP message originally stored on the database.
        /// Optum ACH, Client EFT and Enrolled No-Pays payments are ignored as they are not Azure bound.
        /// </summary>
        /// <param name="request">The list of transaction Ids to send and an optional filter by message type: AP or RP.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>200 if successful; 404 if no messages found; 400 if error.</returns>
        /// <remarks>A transaction Id may have both an AP record and a RP record; use the message type filter to select accordingly.</remarks>
        [HttpPost("SendApReapMessageFromDb")]
        [ProducesResponseType(typeof(ApReapSentCountDto), Status200OK)]
        [ProducesResponseType(Status404NotFound)]
        public async Task<IActionResult> SendApReapMessageFromDb(
            [FromBody][Required] ApReapDbPayload request,
            CancellationToken token = default)
        {
            var allMessages = await _mediator
                .Send(new GetApMessageListByTransactionIds.Query(request.TransactionIds.ToList(), request.ApMessageType), token)
                .ConfigureAwait(false);

            if (allMessages == null || !allMessages.Any())
            {
                return NotFound($"No AP/REAP messages found for the provided transaction IDs and message type");
            }

            // Remove records where the ApRoutingType is not Azure
            var apMessages = allMessages.Where(x => x.ApRoutingType == RoutingTypes.Azure);

            if (apMessages == null || !apMessages.Any())
            {
                return NotFound($"No Azure bound AP/REAP messages found for the provided transaction IDs and message type");
            }

            _logger.LogInformation("{Count} Azure bound AP/REAP messages found for the provided transaction IDs and message type", apMessages.Count());

            foreach (var apMessage in apMessages)
            {
                // Log contents of message
                using var scope = _logger.BeginScope(new Dictionary<string, object>
                {
                    ["UserKey"] = apMessage.UnConsolPayNumber.SanitizeForLogging(256),
                    ["MessageType"] = apMessage.ApMessageType.SanitizeForLogging(256),
                    ["TransactionId"] = apMessage.TransactionId.ToString().SanitizeForLogging(256),
                    //["ReapTransactionId"] = apMessage.ReapTransactionId.ToString().SanitizeForLogging(256)
                });

                // Create AP/REAP accounts payable message from stored record in database.
                VpayApReap message = CreateApReapMessageForAzure(apMessage);

                var sanitizedMessage = StringHelper.SanitizeObjectStrings(message);
                _logger.LogInformation("Converted Message: {ConvertedMessage} (endpoint:SendApReapMessageFromDb)", JsonConvert.SerializeObject(sanitizedMessage));

                var endpoint = await _optumBus.GetSendEndpoint(new Uri($"queue:{_queueNameSettings.AccountPayable}"));

                // Send message to the Azure encoded bus.
                await endpoint.Send(message, token);
            }

            var result = new ApReapSentCountDto()
            {
                Requested = request.TransactionIds.Count(),
                Found = allMessages.Count(),
                Sent = apMessages.Count(),
                NotSent = allMessages.Count() - apMessages.Count(),
            };

            return Ok(result);
        }

        private static VpayApReap CreateApReapMessageForAzure(ApMessage apMessage)
        {
            return new()
            {
                payr835ID = apMessage.PayerId,
                paymentSource = apMessage.PaymentSource,
                paymentType = apMessage.PaymentType,
                payerSubpayerID = apMessage.SubPayerId,
                providerNumber = apMessage.ProviderNumber,
                providerName = apMessage.ProviderName,
                provNPINumber = apMessage.ProviderNpiNumber,
                unConsolPayNumber = apMessage.UnConsolPayNumber,
                unConsolPayAmount = apMessage.UnConsolPayAmount,
                displConsolPayNumber = apMessage.DisplayConsolPayNumber,
                consolAmount = apMessage.ConsolAmount,
                payMethodCode = apMessage.PayMethodCode,
                settlementDate = apMessage.SettlementDate,
                checkNumber = apMessage.CheckNumber,
                returnReasonCode = apMessage.ReturnReasonCode,
                revisedMethodCode = apMessage.RevisedMethodCode,
                reoriginatedDate = apMessage.ReOriginatedDate
            };
        }

        /// <summary>
        /// Update the status of an Inbound Payment record.
        /// </summary>
        /// <param name="userKey">The payment id</param>
        /// <param name="inboundPaymentId">The id of the Inbound Payment record</param>
        /// <param name="inboundPaymentStatus">The new status of the Inbound Payment record</param>
        /// <param name="cancellationToken">A token to cancel the work</param>
        /// <returns></returns>
        [HttpPost("update-payment-status")]
        public async Task<IActionResult> UpdateInboundPaymentStatus(
            string userKey,
            long inboundPaymentId,
            string inboundPaymentStatus,
            CancellationToken cancellationToken = default)
        {
            // Validate the payment status
            if (!PaymentStatus.TryFromName(inboundPaymentStatus, out var status))
            {
                return BadRequest($"Invalid payment status: {inboundPaymentStatus}");
            }

            // Get payment status
            GetInboundPaymentWithClobByIdSql.Query payQuery = new() { InboundPaymentId = inboundPaymentId };
            GetInboundPaymentWithClobByIdSql.Result payRecord = await _mediator
                .Send(payQuery, cancellationToken)
                .ConfigureAwait(false);

            // Validate if the record exists
            if (payRecord == null)
            {
                return NotFound($"Inbound Payment record with ID {inboundPaymentId} not found.");
            }

            // Validate if payment id exists
            if (payRecord.UserKey != userKey)
            {
                return NotFound($"User Key {userKey} does not match value in Inbound Payment record.");
            }

            // Update payment status
            var cmd = new UpdateInboundPaymentStatusSql.Command
            {
                InboundPaymentId = inboundPaymentId,
                Status = status,
                ModifiedBy = _modifiedBy
            };
            await _mediator
                .Send(cmd, cancellationToken)
                .ConfigureAwait(false);

            return Ok();
        }

        /// <summary>
        /// Update the status of an AP Message Tracking record.
        /// </summary>
        /// <param name="userKey">The payment id</param>
        /// <param name="apMessageTrackingId">The id of the AP Message Tracing record</param>
        /// <param name="apMessageTrackingStatus">The new status of the AP Message Tracing record</param>
        /// <param name="cancellationToken">A token to cancel the work</param>
        /// <returns></returns>
        [HttpPost("update-tracking-status")]
        public async Task<IActionResult> UpdateApMessageTrackingStatus(
            string userKey,
            long apMessageTrackingId,
            string apMessageTrackingStatus,
            CancellationToken cancellationToken = default)
        {
            // Validate the payment status
            if (!ApMessageTrackingStatus.TryFromName(apMessageTrackingStatus, out var status))
            {
                return BadRequest($"Invalid tracking status: {apMessageTrackingStatus}");
            }

            // Get payment status
            GetApMessageTrackingByIdSql.Query apQuery = new() { Id = apMessageTrackingId };
            ApMessageTracking apRecord = await _mediator
                .Send(apQuery, cancellationToken)
                .ConfigureAwait(false);

            if (apRecord == null)
            {
                return NotFound($"AP Message Tracking record with ID {apMessageTrackingId} not found.");
            }

            // Get payment status
            GetInboundPaymentWithClobByIdSql.Query payQuery = new() { InboundPaymentId = apRecord.InboundPaymentId };
            GetInboundPaymentWithClobByIdSql.Result payRecord = await _mediator
                .Send(payQuery, cancellationToken)
                .ConfigureAwait(false);

            // Validate if the record exists
            if (payRecord == null)
            {
                return NotFound($"Inbound Payment record with ID {apRecord.InboundPaymentId} not found.");
            }

            // Validate if payment id exists
            if (payRecord.UserKey != userKey)
            {
                return NotFound($"User Key {userKey} does not match value in Inbound Payment record.");
            }

            // Update ApMessageTracking status
            var cmd = new UpdateApMessageTrackingStatus.Command
            {
                ApMessageTrackingId = apMessageTrackingId,
                Status = status,
                ModifiedBy = _modifiedBy
            };
            await _mediator
                .Send(cmd, cancellationToken)
                .ConfigureAwait(false);

            return Ok();
        }

        /// <summary>
        /// Retrieves an 835 outbound file with information stored in the inbound header and outbound clob table
        /// for the passed in Outbound File Id.
        /// </summary>
        /// <param name="outbound835FileId">The File Id of the Outbound File to download.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the work.</param>
        /// <response code="200">If the 835 file is retrieved successfully.</response>
        /// <response code="400">If the request is invalid.</response>
        /// <response code="404">If the Outbound File Id is not found.</response>
        [HttpGet("download-outbound835-file/{outbound835FileId}")]
        [ProducesResponseType(Status200OK)]
        [ProducesResponseType(Status400BadRequest)]
        [ProducesResponseType(Status404NotFound)]
        public async Task<ActionResult> DownloadOutbound835File(
            [Required] long outbound835FileId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting Outbound835 file download from endpoint");
            var outboundFile = await _mediator.Send(new GetOutbound835FileById.Query(outbound835FileId), cancellationToken);

            if (outboundFile == null)
            {
                _logger.LogError("Could not find outbound file with id: {FileId}", outbound835FileId);
                return NotFound($"Unable to locate Outbound File record [{outbound835FileId}]");
            }

            _logger.LogInformation("Found {Count} payments for batch id {FileId}", outboundFile.PaymentIds.Count, outboundFile.Id);

            var header = await _mediator.Send(new GetInboundHeaderByPaymentIdSql.Query(outboundFile.PaymentIds[0]), cancellationToken);
            if (header == null)
            {
                return NotFound($"Unable to locate Inbound Header for Outbound File record [{outbound835FileId}]");
            }

            _logger.LogInformation("Building Outbound835 file for payer alias {PayerAlias}", outboundFile.PayerAlias?.Sanitize().Truncate(256));

            var fileName = $"{outboundFile.PayerAlias}_{DateTime.Now:yyyyMMddHHmmssffff}.txt";

            try
            {
                // Read Header
                var writerStream = new MemoryStream();

                await using (var headerStream = header.HeaderData.ToStream())
                {
                    using var vPay835 = new VPay835Writer(writerStream, encoding: new UTF8Encoding(false));
                    using var inbound835 = new VPay835Reader(headerStream);

                    var inboundIsa = await inbound835.ReadISAAsync().ConfigureAwait(false);
                    var inboundGs = await inbound835.ReadAsync().ConfigureAwait(false) as GS;

                    var now = DateTime.Now;
                    var outboundRefEO = outboundFile.OutboundRefEO;
                    var outboundIsa = GenerateIsa(outboundRefEO, inboundIsa, now);
                    var outboundGs = GenerateGs(outboundIsa, inboundGs, now);

                    await vPay835.WriteISARecordAsync(outboundIsa).ConfigureAwait(false);
                    await vPay835.WriteGSRecordAsync(outboundGs).ConfigureAwait(false);


                    var count = 1;
                    foreach (var paymentId in outboundFile.PaymentIds)
                    {
                        var clobPayment = await _mediator.Send(
                            new GetOutboundPaymentClobByPaymentId.Query(paymentId), cancellationToken);
                        await using var clobStream = clobPayment.Clob.ToStream();
                        using var clob835 = new VPay835Reader(clobStream, noEnvelope: true);

                        var payment = await clob835.Read835Async().ConfigureAwait(false);
                        payment.ST.TransactionSetControlNumber_02 = $"{count:0000}";
                        count++;
                        await vPay835.Write835TransactionAsync(payment).ConfigureAwait(false);
                    }
                }

                // Finalize file
                _logger.LogInformation("Completed Outbound835 file download");

                writerStream.Position = 0;
                var doc = new Outbound835Document()
                {
                    Stream = writerStream,
                    FileName = fileName,
                    ContentType = "text/plain"
                };

                return File(doc.Stream, doc.ContentType, doc.FileName);
            }
            catch (Exception ex)
            {
                return BadRequest($"Unable to download Outbound835 file [{outbound835FileId}]. Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Generates an 835 outbound file from information stored in the inbound header and inbound clob table
        /// for the passed in Inbound Payment Id and carries out the predefined 835 segment modifications.
        /// </summary>
        /// <param name="clientCode">The client code.</param>
        /// <param name="inboundPaymentId">The inbound payment id.</param>
        /// <param name="clobConverter">The clob converter service.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpGet("generate-outbound835-file/{inboundPaymentId}")]
        [ProducesResponseType(Status200OK)]
        [ProducesResponseType(Status400BadRequest)]
        [ProducesResponseType(Status404NotFound)]
        public async Task<ActionResult> GenerateOutbound835File(
            [Required] long inboundPaymentId,
            [Required] string clientCode,
            [FromServices] IClobConverter clobConverter,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting Outbound835 file generation from endpoint");

            var inboundPayment = await GetInboundPaymentFromDb(inboundPaymentId, cancellationToken).ConfigureAwait(false);
            if (inboundPayment == null)
            {
                return NotFound($"Unable to locate Inbound Payment record [{inboundPaymentId}]");
            }

            var apMessageTracking = await GetApMessageTrackingFromDb(inboundPaymentId, cancellationToken).ConfigureAwait(false);
            if (apMessageTracking == null)
            {
                return NotFound($"Unable to locate AP Message Tracking for Inbound Payment record [{inboundPaymentId}]");
            }

            var header = await _mediator.Send(new GetInboundHeaderByPaymentIdSql.Query(inboundPaymentId), cancellationToken);
            if (header == null)
            {
                return NotFound($"Unable to locate Inbound Header for Inbound Payment record [{inboundPaymentId}]");
            }

            _logger.LogInformation("Building Outbound835 file for payment {UserKey}", inboundPayment.UserKey?.Sanitize().Truncate(256));

            var payerAlias = string.IsNullOrWhiteSpace(inboundPayment.PayerAlias) ? "UNKNOWN" : inboundPayment.PayerAlias;
            var fileName = $"{payerAlias}_{DateTime.Now:yyyyMMddHHmmssffff}.txt";
            //var tempPath = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), fileName);

            try
            {
                // Step 1: Update OutboundRefEO
                TS835 ts835 = await clobConverter.ClobTo835(inboundPayment.PaymentClob);
                if (ts835 == null)
                {
                    return BadRequest($"Unable to decode Payment record [{inboundPaymentId}]");
                }

                string paymentRefEO =
                    string.IsNullOrWhiteSpace(ts835.AllREF?.REF_ReceiverIdentification?.MemberGrouporPolicyNumber_02)
                        ? DefaultRevEv
                        : ts835.AllREF?.REF_ReceiverIdentification?.MemberGrouporPolicyNumber_02;

                // Step 2: Read Header
                var writerStream = new MemoryStream();

                await using (var headerStream = header.HeaderData.ToStream())
                {
                    using var vPay835 = new VPay835Writer(writerStream, encoding: new UTF8Encoding(false));
                    using var inbound835 = new VPay835Reader(headerStream);

                    var inboundIsa = await inbound835.ReadISAAsync().ConfigureAwait(false);
                    var inboundGs = await inbound835.ReadAsync().ConfigureAwait(false) as GS;

                    var now = DateTime.Now;
                    var outboundRefEO = paymentRefEO;

                    var outboundIsa = GenerateIsa(outboundRefEO, inboundIsa, now);
                    var outboundGs = GenerateGs(outboundIsa, inboundGs, now);

                    await vPay835.WriteISARecordAsync(outboundIsa).ConfigureAwait(false);
                    await vPay835.WriteGSRecordAsync(outboundGs).ConfigureAwait(false);

                    // Step 3: Update 835
                    ts835 = await Update835(ts835, apMessageTracking, inboundPayment.PayerSubpayerId, clientCode).ConfigureAwait(false);

                    // Step 4: Save 835
                    string outboundPaymentClob = await clobConverter.Ts835ToClob(ts835);

                    await using var clobStream = outboundPaymentClob.ToStream();
                    using var clob835 = new VPay835Reader(clobStream, noEnvelope: true);

                    var payment = await clob835.Read835Async().ConfigureAwait(false);
                    payment.ST.TransactionSetControlNumber_02 = $"{1:0000}";

                    await vPay835.Write835TransactionAsync(payment).ConfigureAwait(false);
                }

                // Step 5: Finalize file
                _logger.LogInformation("Completed Outbound835 for payment {UserKey}", inboundPayment.UserKey?.Sanitize().Truncate(256));

                writerStream.Position = 0;
                var doc = new Outbound835Document()
                {
                    Stream = writerStream,
                    FileName = fileName,
                    ContentType = "text/plain"
                };

                return File(doc.Stream, doc.ContentType, doc.FileName);
            }
            catch (Exception ex)
            {
                return BadRequest($"Unable to generate Outbound835 file [{inboundPaymentId}]. Error: {ex.Message}");
            }
        }

        private static ISA GenerateIsa(string outboundRefEO, ISA inboundIsa, DateTime now)
        {
            return new ISA
            {
                AuthorizationInformationQualifier_1 = "00",
                AuthorizationInformation_2 = new string(' ', 10),
                SecurityInformationQualifier_3 = "00",
                SecurityInformation_4 = new string(' ', 10),
                SenderIDQualifier_5 = "ZZ",
                InterchangeSenderID_6 = inboundIsa.InterchangeSenderID_6.LimitLength(15, 15),
                ReceiverIDQualifier_7 = "ZZ",
                InterchangeReceiverID_8 = outboundRefEO.LimitLength(15, 15),
                InterchangeDate_9 = now.ToString("yyMMdd"),
                InterchangeTime_10 = now.ToString("HHmm"),
                InterchangeControlStandardsIdentifier_11 = "^",
                InterchangeControlVersionNumber_12 = "00501",
                InterchangeControlNumber_13 = inboundIsa.InterchangeControlNumber_13.LimitLength(9, 9),
                AcknowledgementRequested_14 = "0",
                UsageIndicator_15 = inboundIsa.UsageIndicator_15,
                ComponentElementSeparator_16 = ":"
            };
        }

        private static GS GenerateGs(ISA outboundIsa, GS inboundGs, DateTime now)
        {
            return new GS
            {
                CodeIdentifyingInformationType_1 = "HP",
                SenderIDCode_2 = outboundIsa.InterchangeSenderID_6.Trim().LimitLength(2, 15),
                ReceiverIDCode_3 = outboundIsa.InterchangeReceiverID_8.Trim().LimitLength(2, 15),
                Date_4 = now.ToString("yyyyMMdd"),
                Time_5 = now.ToString("HHmm"),
                GroupControlNumber_6 = (inboundGs?.GroupControlNumber_6 ?? "").Trim().LimitLength(1, 9),
                TransactionTypeCode_7 = "X",
                VersionAndRelease_8 = "005010X221A1"
            };
        }

        private static byte[] GetBytesFromString(string str)
        {
            return Encoding.UTF8.GetBytes(str);
        }

        private static byte[] GetBytesFromStream(Stream stream)
        {
            if (stream is MemoryStream memoryStream)
            {
                return memoryStream.ToArray();
            }

            using var outStream = new MemoryStream();
            stream.CopyTo(outStream);
            return outStream.ToArray();
        }

        /// <summary>
        /// Gets the InboundPayment record based on the message.
        /// </summary>
        /// <param name="inboundPaymentId">The inbound payment id.</param>
        /// <param name="cancellationToken">A token to cancel the work.</param>
        /// <returns>The InboundPayment record for the given InboundPaymentId.</returns>
        private async Task<GetInboundPaymentWithClobByIdSql.Result> GetInboundPaymentFromDb(long inboundPaymentId, CancellationToken cancellationToken)
        {
            var inboundPayment = await _mediator.Send(new GetInboundPaymentWithClobByIdSql.Query { InboundPaymentId = inboundPaymentId }, cancellationToken).ConfigureAwait(false);
            return inboundPayment;
        }

        private async Task<ApMessageTracking> GetApMessageTrackingFromDb(long inboundPaymentId, CancellationToken cancellationToken)
        {
            ApMessageTracking apMessageTracking =
                await _mediator.Send(
                    new GetApMessageTrackingByInboundPaymentId.Query { InboundPaymentId = inboundPaymentId }, cancellationToken).ConfigureAwait(false);

            return apMessageTracking;
        }

        private async Task<TS835> Update835(TS835 ts835, ApMessageTracking apMessageTracking, string subpayerId, string clientCode)
        {
            RemoveEoRefs(ts835);

            PaymentInformation bprInfo = await GetPaymentInfoForTransaction(apMessageTracking);

            if (bprInfo.PaymentType != PaymentType.CheckToACH)
            {
                // Update BPR
                ts835.BPR_FinancialInformation = bprInfo.GenerateBPRFromPaymentInfo();

                // Update TRN
                ts835.TRN_ReassociationTraceNumber = bprInfo.GenerateTRN();
            }

            // Update Loop 1000A
            ts835.AllN1 ??= new All_N1_835();

            PayerInformation payerInfo = await GetPayerInfoForTransaction(clientCode, subpayerId).ConfigureAwait(false);
            ts835.AllN1.Loop1000A = payerInfo.GenerateLoop1000A();

            return ts835;
        }

        // Either pull TpaCode from Platform Configuration or Update APMessageTracking to have client code
        private async Task<PayerInformation> GetPayerInfoForTransaction(string clientCode, string subpayerId)
        {
            var payerInfo = await _mediator.Send(new OtbBillingEntityDetailQuery(clientCode, $"1{subpayerId}"));

            if (payerInfo == null || payerInfo.Fields == null || !payerInfo.Fields.Any())
            {
                throw new KeyNotFoundException($"Unable to find OTB Payer Information for ClientCode: [{clientCode}] and SubPayerId: [{subpayerId}]");
            }

            var infoMap = payerInfo.Fields.ToDictionary(x => x.FieldName, x => x.FieldValue);
            var adjustedZipCode = infoMap.GetValueOrDefault("PayerZipCode")?.Replace("-", string.Empty);
            var result = new PayerInformation
            {
                PayerName = infoMap.GetValueOrDefault("BillingEntityName"),
                PayerAddressLine1 = infoMap.GetValueOrDefault("PayerAddressLine1"),
                PayerAddressLine2 = infoMap.GetValueOrDefault("PayerAddressLine2 "),
                PayerCityName = infoMap.GetValueOrDefault("PayerCityName"),
                PayerStateCode = infoMap.GetValueOrDefault("PayerStateCode"),
                PayerZipCode = adjustedZipCode,
                REF_2U_02 = infoMap.GetValueOrDefault("PayerID")?.TrimStart('0'),  // Do not include leading zeros! Must match value from TRN04 without the leading zeros.
                //REF_EO_02 = $"1{subpayerId}",                                    // Do not include field in Outbound 835! See RemoveEoRefs() for more information. Must match the subPayerId preceded with a leading '1'.
                CX_02 = infoMap.GetValueOrDefault("PER_CX_02"),
                CX_04 = infoMap.GetValueOrDefault("PER_CX_03"),
                BL_02 = infoMap.GetValueOrDefault("PER_BL_02"),
                BL_03 = infoMap.GetValueOrDefault("PER_BL_03"),
                BL_04 = infoMap.GetValueOrDefault("PER_BL_04"),
                BL_05 = infoMap.GetValueOrDefault("PER_BL_05"),
                BL_06 = infoMap.GetValueOrDefault("PER_BL_06"),
                BL_07 = infoMap.GetValueOrDefault("PER_BL_07"),
                BL_08 = infoMap.GetValueOrDefault("PER_BL_08"),
            };

            var alreadyHasPhoneNbr = false;
            const string PhoneValue = "TE";
            const string FaxValue = "FX";
            const string EmailValue = "EM";

            // The values we are getting back are missing BL_03, 05, and 07 so try to determine which values to use for them
            if (string.IsNullOrWhiteSpace(result.BL_03) && !string.IsNullOrWhiteSpace(result.BL_04))
            {
                if (IsNumeric(result.BL_04))
                {
                    // TE is recommend 1st so using that.
                    result.BL_03 = PhoneValue;
                    alreadyHasPhoneNbr = true;
                }
                else
                {
                    result.BL_03 = EmailValue;
                }
            }

            if (string.IsNullOrWhiteSpace(result.BL_05) && !string.IsNullOrWhiteSpace(result.BL_06))
            {
                if (IsNumeric(result.BL_06))
                {
                    result.BL_05 = alreadyHasPhoneNbr ? FaxValue : PhoneValue;
                    alreadyHasPhoneNbr = true;
                }
                else
                {
                    result.BL_05 = EmailValue;
                }
            }

            if (string.IsNullOrWhiteSpace(result.BL_07) && !string.IsNullOrWhiteSpace(result.BL_08))
            {
                if (IsNumeric(result.BL_08))
                {
                    result.BL_07 = alreadyHasPhoneNbr ? FaxValue : PhoneValue;
                }
                else
                {
                    result.BL_07 = EmailValue;
                }
            }

            return result;
        }

        private static bool IsNumeric(string val)
        {
            return long.TryParse(val, out var _);
        }

        private static void RemoveEoRefs(TS835 ts835)
        {
            // Need to remove a field under the Loop1000A that is REF*EO. This field is only needed in incoming report. NOTE: Some 835's may not have this field configured.
            var allEoRefs = ts835.AllN1?.Loop1000A?.REF_AdditionalPayerIdentification?.Where(x =>
                x.ReferenceIdentificationQualifier_01.Equals("EO")).ToList();

            if (allEoRefs != null && allEoRefs.Count != 0)
            {
                foreach (REF_AdditionalPayerIdentification allEoRef in allEoRefs)
                {
                    ts835.AllN1.Loop1000A.REF_AdditionalPayerIdentification.Remove(allEoRef);
                }
            }

            if (string.IsNullOrWhiteSpace(ts835.AllREF?.REF_ReceiverIdentification?.MemberGrouporPolicyNumber_02))
            {
                ts835.AllREF ??= new All_REF_835_3();
                ts835.AllREF.REF_ReceiverIdentification ??= new REF_ReceiverIdentification();
                ts835.AllREF.REF_ReceiverIdentification.MemberGrouporPolicyNumber_02 = DefaultRevEv;
            }
        }

        private async Task<PaymentInformation> GetPaymentInfoForTransaction(ApMessageTracking apMessageTracking, CancellationToken token = default)
        {
            // First element is 'TRN00', then TRN01, TRN02, etc...
            // First 2 elements are hard coded, so we don't need those
            string[] trnSegments = apMessageTracking.TrnSegment.Split('*');
            //TODO: should we throw exception if TrnSegment does not contain all parts?

            var result = new PaymentInformation
            {
                PaymentAmount = apMessageTracking.LoadAmount,
                PaymentDate = apMessageTracking.LoadDate.AddDays(1).Date,
                PaymentType = PaymentTypeCodeHelper.StringToPaymentType(apMessageTracking.PaymentTypeCode),
                TRN02 = trnSegments[2],
                TRN03 = trnSegments[3],
                TRN04 = trnSegments[4]
            };

            if (result.PaymentType == PaymentType.ACH)
            {
                var paymentAccountInfo = await _mediator.Send(new TransactionBankAccountsQuery(apMessageTracking.TransactionId), token).ConfigureAwait(false);
                //TODO: should we throw exception if paymentAccountInfo returns null?

                if (paymentAccountInfo != null)
                {
                    result.PayersBankAccountNumber = paymentAccountInfo.FromAccountNumber;
                    result.PayersRoutingNumber = paymentAccountInfo.FromRouteNumber;
                    result.ReceiverBankAccountNumber = paymentAccountInfo.ToAccountNumber;
                    result.ReceiverRoutingNumber = paymentAccountInfo.ToRouteNumber;
                }
            }

            return result;
        }
    }
}
