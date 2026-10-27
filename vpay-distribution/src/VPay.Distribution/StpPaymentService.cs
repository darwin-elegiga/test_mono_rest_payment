using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VPay.Distribution.Common;
using VPay.Distribution.Common.Interfaces;
using VPay.Distribution.Common.Models;
using VPay.Distribution.Http.Abstractions;
using VPay.DocSys.Data;

namespace VPay.Distribution
{
    public class StpPaymentService : IStpPaymentService
    {
        private readonly IStpPaymentRepository _repository;
        private readonly IPmbMessagePublisher _pmbPublisher;
        private readonly IPaymentRoutingApi _paymentRoutingApi;
        private readonly IRemittanceService _remittanceService;
        private readonly IStorageService _storageService;
        private readonly ILogger<StpPaymentService> _logger;

        public StpPaymentService(
            IStpPaymentRepository repository,
            IPmbMessagePublisher pmbPublisher,
            IPaymentRoutingApi paymentRoutingApi,
            IRemittanceService remittanceService,
            IStorageService storageService,
            ILogger<StpPaymentService> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _pmbPublisher = pmbPublisher ?? throw new ArgumentNullException(nameof(pmbPublisher));
            _paymentRoutingApi = paymentRoutingApi ?? throw new ArgumentNullException(nameof(paymentRoutingApi));
            _remittanceService = remittanceService ?? throw new ArgumentNullException(nameof(remittanceService));
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Stp Payments

        /// <summary>
        /// Get STP payment by Transaction ID.
        /// </summary>
        /// <param name="transactionId">The transaction Id of the STP payment.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The STP payment for the Transaction Id if record exists; Null otherwise.</returns>
        public async Task<StpPayment> GetStpPayment(long transactionId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _repository.GetStpPayment(transactionId, cancellationToken);
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "ERROR retrieving STP payment for Transaction ID {TransactionId}", transactionId);
                _logger.LogError(ex, "ERROR retrieving STP payment");
                throw;
            }
        }

        /// <summary>
        /// Get STP payments by a list of Transaction IDs.
        /// </summary>
        /// <param name="transactionIds">The list of transaction Ids of the STP payments.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The list of STP payments for the Transaction Ids if records exist; Empty list otherwise.</returns>
        public async Task<IEnumerable<StpPayment>> GetStpPayments(IEnumerable<long> transactionIds, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _repository.GetStpPayments(transactionIds, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR retrieving STP payments for Transaction IDs {TransactionIds}", string.Join(", ", transactionIds));
                throw;
            }
        }

        /// <summary>
        /// Get list of STP Payments that match the search criteria for <see cref="StpPaymentFilter"/> class.
        /// </summary>
        /// <param name="networkName">Filter by network partner name. Null for all networks.</param>
        /// <param name="enrollType">Filter by enrollment type: B2B or C2B. Null for all enrollment types.</param>
        /// <param name="edi835Type">The type of EDI 835 to filter by: PRS or SDS. Null for all EDI 835 types.</param>
        /// <param name="edi835Avail">Filter for STP payments that have an available EDI 835. Null to ignore flag.</param>
        /// <param name="remitValid">Filter for STP payments for which the Remit has been marked as valid. Null to ignore flag.</param>
        /// <param name="paymentSent">Filter for STP payments that have been marked as sent. Null to ignore flag.</param>
        /// <param name="pmbMessageSent">Filter for STP payments that have had their PMB message sent. Null to ignore flag.</param>
        /// <param name="paymentSentRetries">Filter for STP payments that have a given number of payment send retries. Null for any retry count.</param>
        /// <param name="maxResults">The number of max records to return. The default is 10,000.</param>
        /// <param name="cancellationToken">The token to cancel the request.</param>
        /// <returns>The list of STP payments that match the requested criteria if any; Empty list otherwise.</returns>
        public async Task<IList<StpPayment>> SearchStpPayments(
            string networkName = null,
            string enrollType = null,
            string edi835Type = null,
            bool? edi835Avail = null,
            bool? remitValid = null,
            bool? paymentSent = null,
            bool? pmbMessageSent = null,
            int? paymentSentRetries = null,
            int maxResults = 10000,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await _repository.SearchStpPayments(networkName, enrollType, edi835Type, edi835Avail, remitValid, paymentSent, pmbMessageSent, paymentSentRetries, maxResults, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR searching STP payments with provided criteria");
                throw;
            }
        }

        #endregion

        #region STP Payment Status

        /// <summary>
        /// Mark the STP payment as having a valid Remit document.
        /// </summary>
        /// <param name="transactionId">The transaction ID of the STP payment.</param>
        /// <param name="timestamp">The date and time when the Remit was validated. Sets the RemitValidDateTime. Null for current date.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The STP payment showing the modified fields, if modified; Null otherwise.</returns>
        public async Task<StpPayment> MarkRemitValid(long transactionId, DateTime? timestamp = null, CancellationToken cancellationToken = default)
        {
            try
            {
                //_logger.LogInformation("Marking STP payment as Remit Valid for Transaction ID {TransactionId}", transactionId);
                _logger.LogInformation("Marking STP payment as Remit Valid");
                return await _repository.MarkRemitValid(transactionId, timestamp, cancellationToken);
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "ERROR marking STP payment as Remit Valid for Transaction ID {TransactionId}", transactionId);
                _logger.LogError(ex, "ERROR marking STP payment as Remit Valid");
                throw;
            }
        }

        /// <summary>
        /// Mark the STP Payment as having an available 835 file.
        /// </summary>
        /// <param name="transactionId">The transaction Id.</param>
        /// <param name="timestamp">The date and time when the EDI 835 became available. Sets the Edi835AvailDateTime. Null for current date.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The STP payment showing the modified fields, if modified; Null otherwise.</returns>
        public async Task<StpPayment> MarkEdi835Avail(long transactionId, DateTime? timestamp = null, CancellationToken cancellationToken = default)
        {
            try
            {
                //_logger.LogInformation("Marking STP payment as EDI 835 Available for Transaction ID {TransactionId}", transactionId);
                _logger.LogInformation("Marking STP payment as EDI 835 Available");
                return await _repository.MarkEdi835Avail(transactionId, timestamp, cancellationToken);
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "ERROR marking STP payment as EDI 835 Available for Transaction ID {TransactionId}", transactionId);
                _logger.LogError(ex, "ERROR marking STP payment as EDI 835 Available");
                throw;
            }
        }

        /// <summary>
        /// Mark the STP Payment as having been successfully sent out.
        /// </summary>
        /// <param name="transactionId">The transaction Id.</param>
        /// <param name="timestamp">The date and time when the payment was sent. Sets the PaymentSentDateTime. Null for current date.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The STP payment showing the modified fields, if modified; Null otherwise.</returns>
        public async Task<StpPayment> MarkPaymentSent(long transactionId, DateTime? timestamp = null, CancellationToken cancellationToken = default)
        {
            try
            {
                //_logger.LogInformation("Marking STP payment as Payment Sent for Transaction ID {TransactionId}", transactionId);
                _logger.LogInformation("Marking STP payment as Payment Sent");
                return await _repository.MarkPaymentSent(transactionId, timestamp, cancellationToken);
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "ERROR marking STP payment as Payment Sent for Transaction ID {TransactionId}", transactionId);
                _logger.LogError(ex, "ERROR marking STP payment as Payment Sent");
                throw;
            }
        }

        /// <summary>
        /// Mark the STP Payment as having an error while trying to be sent out and increases the retry count.
        /// </summary>
        /// <param name="transactionId">The transaction Id.</param>
        /// <param name="timestamp">The date and time when the payment was tried but not sent. Sets the PaymentSentDateTime. Null for current date.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The STP payment showing the modified fields, if modified; Null otherwise.</returns>
        public async Task<StpPayment> MarkPaymentNotSent(long transactionId, DateTime? timestamp = null, CancellationToken cancellationToken = default)
        {
            try
            {
                //_logger.LogInformation("Marking STP payment as Payment Not Sent for Transaction ID {TransactionId}", transactionId);
                _logger.LogInformation("Marking STP payment as Payment Not Sent");
                return await _repository.MarkPaymentNotSent(transactionId, timestamp, cancellationToken);
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "ERROR marking STP payment as Payment Not Sent for Transaction ID {TransactionId}", transactionId);
                _logger.LogError(ex, "ERROR marking STP payment as Payment Not Sent");
                throw;
            }
        }

        /// <summary>
        /// Mark the STP Payment as having successfully sent its PMB message.
        /// </summary>
        /// <param name="transactionId">The transaction Id.</param>
        /// <param name="timestamp">The date and time when the payment was sent. Sets the PmbMessageSentDateTime. Null for current date.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The STP payment showing the modified fields, if modified; Null otherwise.</returns>
        public async Task<StpPayment> MarkPmbMessageSent(long transactionId, DateTime? timestamp = null, CancellationToken cancellationToken = default)
        {
            try
            {
                //_logger.LogInformation("Marking STP payment PMB message as Sent for Transaction ID {TransactionId}", transactionId);
                _logger.LogInformation("Marking STP payment PMB message as Sent");
                return await _repository.MarkPmbMessageSent(transactionId, timestamp, cancellationToken);
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "ERROR marking STP payment PMB message as Not Sent for Transaction ID {TransactionId}", transactionId);
                _logger.LogError(ex, "ERROR marking STP payment PMB message as Not Sent");
                throw;
            }
        }

        /// <summary>
        /// Mark the STP Payment as having an error while trying to send PMB message.
        /// </summary>
        /// <param name="transactionId">The transaction Id.</param>
        /// <param name="timestamp">The date and time when the payment was tried but not sent. Sets the PmbMessageSentDateTime. Null for current date.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The STP payment showing the modified fields, if modified; Null otherwise.</returns>
        public async Task<StpPayment> MarkPmbMessageNotSent(long transactionId, DateTime? timestamp = null, CancellationToken cancellationToken = default)
        {
            try
            {
                //_logger.LogInformation("Marking STP payment PMB message as Not Sent for Transaction ID {TransactionId}", transactionId);
                _logger.LogInformation("Marking STP payment PMB message as Not Sent");
                return await _repository.MarkPmbMessageNotSent(transactionId, timestamp, cancellationToken);
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "ERROR marking STP payment PMB message as Not Sent for Transaction ID {TransactionId}", transactionId);
                _logger.LogError(ex, "ERROR marking STP payment PMB message as Not Sent");
                throw;
            }
        }

        /// <summary>
        /// Reset the sent retries count to zero for the given STP payment.
        /// </summary>
        /// <param name="transactionId">The transaction Id.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The STP payment showing the modified fields, if modified; Null otherwise.</returns>
        public async Task<StpPayment> ResetStpRemitSentRetries(long transactionId, CancellationToken cancellationToken = default)
        {
            try
            {
                var stp = await _repository.GetStpPayment(transactionId, cancellationToken);

                //_logger.LogInformation("Resetting STP payment Remit sent retry count for Transaction ID {TransactionId}", transactionId);
                _logger.LogInformation("Resetting STP payment Remit sent retry count");

                return await _repository.UpdateStpPayment(
                    transactionId,
                    networkName: stp.NetworkName,
                    enrollType: stp.EnrollType,
                    edi835Type: stp.Edi835Type,
                    remitValid: stp.RemitValid,
                    edi835Avail: stp.Edi835Avail,
                    paymentSent: stp.PaymentSent,
                    pmbMessageSent: stp.PmbMessageSent,
                    remitValidDateTime: stp.RemitValidDateTime,
                    edi835AvailDateTime: stp.Edi835AvailDateTime,
                    paymentSentDateTime: stp.PaymentSentDateTime,
                    pmbMessageSentDateTime: stp.PmbMessageSentDateTime,
                    paymentSentRetries: 0,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "ERROR resetting STP payment send retries for Transaction ID {TransactionId}", transactionId);
                _logger.LogError(ex, "ERROR resetting STP payment send retries");
                throw;
            }
        }

        #endregion

        #region PMB Messages

        /// <summary>
        /// Create a PMB message tailored for an STP payment for the given transaction Id.
        /// </summary>
        /// <param name="transactionId">The transaction Id.</param>
        /// <returns>A new PMB message pre-populated with the relevant STP constants.</returns>
        public PmbMessage CreatePmbMessage(long transactionId)
        {
            var pmbMessage = new PmbMessage()
            {
                FromSystem = "FAXNET",
                ToSystem = "FAXISERIES",
                Family = "FAXDIST",
                Data = JsonConvert.SerializeObject(new
                {
                    TransactionId = transactionId,
                    Status = "SENTSTP",
                    TimeStamp = DateTime.Now.ToString("yyyy-MM-dd-HH.mm.ss.ffffff"),
                })
            };
            return pmbMessage;
        }

        /// <summary>
        /// Publish a PMB message tailored for an STP payment for the given transaction Id.
        /// </summary>
        /// <param name="transactionId">The transaction Id.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>True if message was successfully sent; False otherwise.</returns>
        public async Task<bool> PublishPmbMessage(long transactionId, CancellationToken cancellationToken = default)
        {
            try
            {
                var pmbMessage = CreatePmbMessage(transactionId);
                await _pmbPublisher.SendAsync(pmbMessage, cancellationToken);

                //_logger.LogInformation("Published PMB message for STP payment with Transaction ID {TransactionId}", transactionId);
                _logger.LogInformation("Published PMB message for STP payment");
                return true;
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "ERROR publishing PMB message for STP payment with Transaction ID {TransactionId}", transactionId);
                _logger.LogError(ex, "ERROR publishing PMB message for STP payment");
                return false;
            }
        }

        /// <summary>
        /// Send PMB messages for pending STP payments and mark the STP payment as sent if successful.
        /// </summary>
        /// <param name="payment">The STP payment to send.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. The operation will terminate early if the token is canceled.</param>
        /// <returns>True if message was successfully sent; False otherwise.</returns>
        public async Task<bool> SendPmbMessage(StpPayment payment, CancellationToken cancellationToken = default)
        {
            // Send PMB message to iSeries.
            _logger.LogInformation("Sending PMB message for STP payment with Transaction ID {TransactionId} ({EnrollmentType}, {FileType})", payment.TransactionId, payment.EnrollType, payment.Edi835Type);

            var result = await PublishPmbMessage(payment.TransactionId, cancellationToken)
                .ConfigureAwait(false);

            await MarkPmbMessageSent(payment.TransactionId, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return result;
        }

        #endregion

        #region Pending STP Payments

        /// <summary>
        /// Start processing pending STP payments that are missing 835s and/or need to be sent out.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the work.</param>
        public async Task StartPendingToSend(CancellationToken cancellationToken = default)
        {
            await ProcessPendingSdsPayments(cancellationToken).ConfigureAwait(false);

            await ProcessPendingPrsPayments(cancellationToken).ConfigureAwait(false);

            //await ProcessPendingPmbMessages(cancellationToken).ConfigureAwait(false);
        }

        private async Task ProcessPendingSdsPayments(CancellationToken cancellationToken)
        {
            // Find 835s for SDS B2B STP payments that have a valid remit.
            var pendingSds835s = await SearchStpPayments(
                enrollType: EnrollTypes.B2B,
                edi835Type: Edi835Types.SDS,
                edi835Avail: null,
                remitValid: true,
                paymentSent: false,
                cancellationToken: cancellationToken);

            foreach (var payment in pendingSds835s)
            {
                bool exists835 = payment.Edi835Avail;
                if (!exists835)
                {
                    // Verify 835 exists in Document System Storage and if found mark 835 as available.
                    var sds835 = await CanDownloadSds835(payment.TransactionId, cancellationToken).ConfigureAwait(false);
                    exists835 = sds835.Exists;
                }

                if (exists835)
                {
                    _logger.LogInformation("SDS 835 available for Transaction ID {TransactionId}", payment.TransactionId);
                    await ProcessPendingStpPayment(payment, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    _logger.LogInformation("SDS 835 not available for Transaction ID {TransactionId}", payment.TransactionId);
                }
            }
        }

        private async Task ProcessPendingPrsPayments(CancellationToken cancellationToken)
        {
            // Find 835s for PRS B2B STP payments that have a valid remit
            var pendingPrs835s = await SearchStpPayments(
                enrollType: EnrollTypes.B2B,
                edi835Type: Edi835Types.PRS,
                edi835Avail: null,
                remitValid: true,
                paymentSent: false,
                cancellationToken: cancellationToken);

            foreach (var payment in pendingPrs835s)
            {
                bool exists835 = payment.Edi835Avail;
                if (!exists835)
                {
                    // Verify 835 exists in Payment Routing API and if found mark 835 as available.
                    var prs835 = await CanDownloadPrs835(payment.TransactionId, cancellationToken).ConfigureAwait(false);
                    exists835 = prs835.Exists;
                }

                if (exists835)
                {
                    _logger.LogInformation("PRS 835 available for Transaction ID {TransactionId}", payment.TransactionId);
                    await ProcessPendingStpPayment(payment, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    _logger.LogInformation("PRS 835 not available for Transaction ID {TransactionId}", payment.TransactionId);
                }
            }
        }

        private async Task ProcessPendingPmbMessages(CancellationToken cancellationToken)
        {
            // Find STP payments that have not had their PMB message sent yet.
            var pendingPmbMessages = await SearchStpPayments(
                edi835Avail: null,
                remitValid: true,
                paymentSent: true,
                pmbMessageSent: false,
                cancellationToken: cancellationToken);

            foreach (var payment in pendingPmbMessages)
            {
                await SendPmbMessage(payment, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Process a pending STP payment by marking the associated 835 document as available and posting the
        /// remittance document to notify partner networks that the 835 document can be downloaded.
        /// </summary>
        /// <remarks>This method performs three main actions for the specified STP payment: 1. Marks the EDI
        /// 835 document as available. 2. Posts the remittance document. 3. Marks the payment as sent if posting was valid.
        /// Logging is performed at the debug level to provide visibility into the processing steps,
        /// including the transaction ID, enrollment type, and file type.</remarks>
        /// <param name="payment">The STP payment to process. This includes details such as the transaction ID, enrollment type,  and EDI 835 type.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. The operation will terminate early if the token is canceled.</param>
        /// <returns></returns>
        public async Task ProcessPendingStpPayment(
            StpPayment payment,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(payment);

            if (payment.PaymentSentRetries >= 6)
            {
                // Too many retries, nothing to do.
                return;
            }

            // Mark payment as having available 835 file.
            _logger.LogInformation("Processing STP payment with Transaction ID {TransactionId} ({EnrollmentType}, {FileType})", payment.TransactionId, payment.EnrollType, payment.Edi835Type);

            if (!payment.Edi835Avail)
            {
                await MarkEdi835Avail(payment.TransactionId, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }

            _logger.LogInformation("Posting remit document for STP payment with Transaction ID {TransactionId} ({EnrollmentType}, {FileType})", payment.TransactionId, payment.EnrollType, payment.Edi835Type);

            // Prepare JSON payload for Remit Partner Network API Payment B2B Post endpoint.          
            var json = await _storageService.GetRemitJson(payment.TransactionId, cancellationToken).ConfigureAwait(false);
            var valid = await _remittanceService.PostRemitDocument(payment.TransactionId, payment.EnrollType, json, cancellationToken).ConfigureAwait(false);

            if (valid)
            {
                // Mark payment as sent to partner network.
                await MarkPaymentSent(payment.TransactionId, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                // Handle errors and retries.
                // Mark payment as not sent to partner network and increase the number of retries.
                await MarkPaymentNotSent(payment.TransactionId, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }

            // Send PMB message to iSeries.
            await SendPmbMessage(payment, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region 835 Documents

        /// <summary>
        /// Verify if the 835 document for the given SDS STP payment can be downloaded via Document System Storage.
        /// </summary>
        /// <param name="transactionId">The transaction Id.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. The operation will terminate early if the token is canceled.</param>
        /// <returns>The result of the download check.</returns>
        public async Task<TransactionExistsDto> CanDownloadSds835(
            long transactionId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _storageService
                    .DownloadEdi835(transactionId, cancellationToken)
                    .ConfigureAwait(false);

                if (!string.IsNullOrWhiteSpace(result))
                {
                    return await Task.FromResult(new TransactionExistsDto { TransactionId = transactionId, Exists = true });
                }
                else
                {
                    _logger.LogError("ERROR occurred during call to download 835 document for SDS payment");
                    return new TransactionExistsDto { TransactionId = transactionId, Exists = false, Message = "EDI 835 contents are null or empty." };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR occurred during call to download 835 document for SDS payment");
                return new TransactionExistsDto { TransactionId = transactionId, Exists = false, Message = ex.Message };
            }
        }

        /// <summary>
        /// Verify if the 835 document for the given PRS STP payment can be downloaded via Payment Routing API.
        /// </summary>
        /// <param name="transactionId">The transaction Id.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. The operation will terminate early if the token is canceled.</param>
        /// <returns>The result of the download check.</returns>
        public async Task<TransactionExistsDto> CanDownloadPrs835(
            long transactionId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _paymentRoutingApi
                    .CanDownloadOutbound835Document(transactionId, cancellationToken)
                    .ConfigureAwait(false);

                return new TransactionExistsDto { TransactionId = result.TransactionId, Exists = result.Exists };
            }
            catch (Refit.ApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogInformation("NOT FOUND during call to download 835 document for PRS payment ({StatusCode}): {ErrorMessage}", ex.StatusCode, ex.Message);
                    return new TransactionExistsDto { TransactionId = transactionId, Exists = false, Message = ex.Message };
                }

                _logger.LogError(ex, "ERROR during call to download 835 document for PRS payment ({StatusCode}): {ErrorMessage}", ex.StatusCode, ex.Message);
                return new TransactionExistsDto { TransactionId = transactionId, Exists = false, Message = ex.Message };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR occurred during call to download 835 document for PRS payment");
                return new TransactionExistsDto { TransactionId = transactionId, Exists = false, Message = ex.Message };
            }
        }

        #endregion
    }
}
