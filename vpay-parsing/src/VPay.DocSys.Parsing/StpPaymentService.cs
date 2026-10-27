using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VPay.DocSys.Data;
using VPay.DocSys.Parsing.Common.Extensions;
using VPay.DocSys.Parsing.Common.Interfaces;

namespace VPay.DocSys.Parsing
{
    public class StpPaymentService : IStpPaymentService
    {
        private readonly IStpPaymentRepository _repository;
        private readonly IStatusFileTracker _tracker;
        private readonly ILogger<StpPaymentService> _logger;

        public StpPaymentService(
            IStpPaymentRepository repository,
            IStatusFileTracker tracker,
            ILogger<StpPaymentService> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Get STP payment by Transaction ID. The caller can choose to use the Repository in DocSys Data
        /// or the local SQL commands in Status File Tracker by setting the useRepository flag.
        /// </summary>
        /// <param name="transactionId">The transaction Id of the STP payment.</param>
        /// <param name="useRepository">The flag to choose to use the Repository in DocSys Data or Status File Tracker SQL commands.
        /// The default is false to use the local SQL commands in Status File Tracker for better performance.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The STP payment for the Transaction Id if record exists; Null otherwise.</returns>
        /// <remarks>The STP payment service uses the local SQL commands in <see cref="IStatusFileTracker"/> by default.</remarks>
        public async Task<StpPayment> GetStpPayment(long transactionId, bool useRepository = false, CancellationToken cancellationToken = default)
        {
            try
            {
                if (useRepository)
                {
                    return await _repository.GetStpPayment(transactionId, cancellationToken);
                }

                return _tracker.GetStpPayment(transactionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving STP payment for Transaction ID {TransactionId}",
                    transactionId.Sanitize());
                throw;
            }
        }

        /// <summary>
        /// Get STP payments by a list of Transaction IDs. The caller can choose to use the Repository in DocSys Data
        /// or the local SQL commands in Status File Tracker by setting the useRepository flag.
        /// </summary>
        /// <param name="transactionIds">The list of transaction Ids of the STP payments.</param>
        /// <param name="useRepository">The flag to choose to use the Repository in DocSys Data or Status File Tracker SQL commands.
        /// The default is false to use the local SQL commands in Status File Tracker for better performance.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The list of STP payments for the Transaction Ids if records exist; Empty list otherwise.</returns>
        public async Task<IEnumerable<StpPayment>> GetStpPayments(IEnumerable<long> transactionIds, bool useRepository = false, CancellationToken cancellationToken = default)
        {
            try
            {
                if (useRepository)
                {
                    return await _repository.GetStpPayments(transactionIds, cancellationToken);
                }

                return _tracker.GetStpPayments(transactionIds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving STP payments for Transaction IDs {TransactionIds}",
                    string.Join(", ", transactionIds.Sanitize()));
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
        /// <param name="pmbMessageSent">Filter for STP payments that have been marked as sent via PMB Message. Null to ignore flag.</param>
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
                _logger.LogError(ex, "Error searching STP payments with provided criteria");
                throw;
            }
        }

        /// <summary>
        /// Insert multiple STP payments into StpPayment table using SQL Bulk Copy.
        /// All payments will be created with the same network name, enrollment type, and EDI 835 type.
        /// </summary>
        /// <param name="transactionIds">The list of transaction Ids for the payments to insert.</param>
        /// <param name="networkName">The network partner name. Required.</param>
        /// <param name="enrollType">The enrollment type: B2B or C2B. Required.</param>
        /// <param name="edi835Type">The type of EDI 835 to use: SDS or PRS. Required.</param>
        /// <param name="remitValid">Indicates if the remit is valid. The default is false.</param>
        /// <param name="edi835Avail">Indicates if the EDI 835 is available. The default is false.</param>
        /// <param name="paymentSent">Indicates if the payment has been sent. The default is false.</param>
        /// <param name="pmbMessageSent">The PMB message sent status. Default is false.</param>
        /// <exception cref="DataException"></exception>
        public Task<int> InsertStpPayments(
            IEnumerable<long> transactionIds,
            string networkName,
            string enrollType,
            string edi835Type,
            bool remitValid = false,
            bool edi835Avail = false,
            bool paymentSent = false,
            bool pmbMessageSent = false,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Exclude existing payments with the same transaction IDs to avoid duplicate key errors during insert.
                // This can happens for example after running Clean Docs.
                var existingPayments = _tracker.GetStpPayments(transactionIds);
                var newTransactionIds = transactionIds.Except(existingPayments.Select(p => p.TransactionId)).ToList();

                if (newTransactionIds.Count == 0)
                {
                    _logger.LogInformation("No new STP payments to insert after excluding existing payments with the same Transaction IDs.");
                    return Task.FromResult(0);
                }

                // Use Bulk Insert from StatusFileTracker for performance reasons.
                _tracker.BulkInsertStpPayments(newTransactionIds, networkName, enrollType, edi835Type, remitValid, edi835Avail, paymentSent, pmbMessageSent);
                return Task.FromResult(newTransactionIds.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting STP payments");
                throw;
            }
        }

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
                return await _repository.MarkRemitValid(transactionId, timestamp, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking STP payment as Remit Valid for Transaction ID {TransactionId}", transactionId.Sanitize());
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
                return await _repository.MarkEdi835Avail(transactionId, timestamp, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking STP payment as EDI 835 Available for Transaction ID {TransactionId}", transactionId.Sanitize());
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
                return await _repository.MarkPaymentSent(transactionId, timestamp, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking STP payment as Payment Sent for Transaction ID {TransactionId}", transactionId.Sanitize());
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
                return await _repository.MarkPaymentNotSent(transactionId, timestamp, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking STP payment as Payment Not Sent for Transaction ID {TransactionId}", transactionId.Sanitize());
                throw;
            }
        }
    }
}
