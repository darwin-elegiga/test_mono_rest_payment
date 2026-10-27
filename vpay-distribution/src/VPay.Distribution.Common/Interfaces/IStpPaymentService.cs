using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VPay.Distribution.Common.Models;
using VPay.DocSys.Data;

namespace VPay.Distribution.Common
{
    public interface IStpPaymentService
    {
        /// <summary>
        /// Get STP payment by Transaction ID.
        /// </summary>
        /// <param name="transactionId">The transaction Id of the STP payment.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The STP payment for the Transaction Id if record exists; Null otherwise.</returns>
        Task<StpPayment> GetStpPayment(long transactionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get STP payments by a list of Transaction IDs.
        /// </summary>
        /// <param name="transactionIds">The list of transaction Ids of the STP payments.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The list of STP payments for the Transaction Ids if records exist; Empty list otherwise.</returns>
        Task<IEnumerable<StpPayment>> GetStpPayments(IEnumerable<long> transactionIds, CancellationToken cancellationToken = default);

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
        Task<IList<StpPayment>> SearchStpPayments(string networkName = null, string enrollType = null, string edi835Type = null, bool? edi835Avail = null, bool? remitValid = null, bool? paymentSent = null, bool? pmbMessageSent = null, int? paymentSentRetries = null, int maxResults = 10000, CancellationToken cancellationToken = default);

        /// <summary>
        /// Mark the STP payment as having a valid Remit document.
        /// </summary>
        /// <param name="transactionId">The transaction ID of the STP payment.</param>
        /// <param name="timestamp">The date and time when the Remit was validated. Sets the RemitValidDateTime. Null for current date.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The STP payment showing the modified fields, if modified; Null otherwise.</returns>
        Task<StpPayment> MarkRemitValid(long transactionId, DateTime? timestamp = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Mark the STP Payment as having an available 835 file.
        /// </summary>
        /// <param name="transactionId">The transaction Id.</param>
        /// <param name="timestamp">The date and time when the EDI 835 became available. Sets the Edi835AvailDateTime. Null for current date.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The STP payment showing the modified fields, if modified; Null otherwise.</returns>
        Task<StpPayment> MarkEdi835Avail(long transactionId, DateTime? timestamp = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Mark the STP Payment as having been successfully sent out.
        /// </summary>
        /// <param name="transactionId">The transaction Id.</param>
        /// <param name="timestamp">The date and time when the payment was sent. Sets the PaymentSentDateTime. Null for current date.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The STP payment showing the modified fields, if modified; Null otherwise.</returns>
        Task<StpPayment> MarkPaymentSent(long transactionId, DateTime? timestamp = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Mark the STP Payment as having an error while trying to be sent out and increases the retry count.
        /// </summary>
        /// <param name="transactionId">The transaction Id.</param>
        /// <param name="timestamp">The date and time when the payment was tried but not sent. Sets the PaymentSentDateTime. Null for current date.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The STP payment showing the modified fields, if modified; Null otherwise.</returns>
        Task<StpPayment> MarkPaymentNotSent(long transactionId, DateTime? timestamp = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reset the STP Payment Remit sent retries count to zero so it can be retried again.
        /// </summary>
        /// <param name="transactionId">The transaction Id.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The STP payment showing the modified fields, if modified; Null otherwise.</returns>
        Task<StpPayment> ResetStpRemitSentRetries(long transactionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Mark the STP Payment as having successfully sent its PMB message.
        /// </summary>
        /// <param name="transactionId">The transaction Id.</param>
        /// <param name="timestamp">The date and time when the payment was sent. Sets the PmbMessageSentDateTime. Null for current date.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The STP payment showing the modified fields, if modified; Null otherwise.</returns>
        Task<StpPayment> MarkPmbMessageSent(long transactionId, DateTime? timestamp = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Mark the STP Payment as having an error while trying to send PMB message.
        /// </summary>
        /// <param name="transactionId">The transaction Id.</param>
        /// <param name="timestamp">The date and time when the payment was tried but not sent. Sets the PmbMessageSentDateTime. Null for current date.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The STP payment showing the modified fields, if modified; Null otherwise.</returns>
        Task<StpPayment> MarkPmbMessageNotSent(long transactionId, DateTime? timestamp = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Create a PMB message tailored for an STP payment for the given transaction Id.
        /// </summary>
        /// <param name="transactionId">The transaction Id.</param>
        /// <returns>A new PMB message pre-populated with the relevant STP constants.</returns>
        PmbMessage CreatePmbMessage(long transactionId);

        /// <summary>
        /// Publish a PMB message tailored for an STP payment for the given transaction Id.
        /// </summary>
        /// <param name="transactionId">The transaction Id.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>True if message was successfully sent; False otherwise.</returns>
        Task<bool> PublishPmbMessage(long transactionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Send PMB messages for pending STP payments and mark the STP payment as sent if successful.
        /// </summary>
        /// <param name="payment">The STP payment to send.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. The operation will terminate early if the token is canceled.</param>
        /// <returns>True if message was successfully sent; False otherwise.</returns>
        Task<bool> SendPmbMessage(StpPayment payment, CancellationToken cancellationToken = default);

        /// <summary>
        /// Start processing pending STP payments that are missing 835s and/or need to be sent out.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the work.</param>
        Task StartPendingToSend(CancellationToken cancellationToken = default);

        /// <summary>
        /// Process a pending STP payment by marking the associated EDI 835 document as available and posting the
        /// remittance document to notify partner networks that the EDI 835 can be downloaded.
        /// </summary>
        /// <remarks>This method performs two main actions for the specified STP payment: 1. Marks the EDI
        /// 835 document as available. 2. Posts the remittance document.  Logging is performed at the debug level to
        /// provide visibility into the processing steps, including  the transaction ID, enrollment type, and file
        /// type.</remarks>
        /// <param name="payment">The STP payment to process. This includes details such as the transaction ID, enrollment type,  and EDI 835 type.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. The operation will terminate early if the token is canceled.</param>
        /// <returns></returns>
        Task ProcessPendingStpPayment(StpPayment payment, CancellationToken cancellationToken = default);

        /// <summary>
        /// Verify if the 835 document for the given SDS STP payment can be downloaded via Document System Storage.
        /// </summary>
        /// <param name="transactionId">The transaction Id.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. The operation will terminate early if the token is canceled.</param>
        /// <returns>The result of the download check.</returns>
        Task<TransactionExistsDto> CanDownloadSds835(long transactionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Verify if the 835 document for the given PRS STP payment can be downloaded via Payment Routing API.
        /// </summary>
        /// <param name="transactionId">The transaction Id.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. The operation will terminate early if the token is canceled.</param>
        /// <returns>The result of the download check.</returns>
        Task<TransactionExistsDto> CanDownloadPrs835(long transactionId, CancellationToken cancellationToken = default);
    }
}
