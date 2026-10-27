using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VPay.DocSys.Data;

namespace VPay.DocSys.Parsing.Common.Interfaces
{
    public interface IStpPaymentService
    {
        /// <summary>
        /// Get STP payment by Transaction ID.
        /// </summary>
        /// <param name="transactionId">The transaction Id of the STP payment.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The STP payment for the Transaction Id if record exists; Null otherwise.</returns>
        Task<StpPayment> GetStpPayment(long transactionId, bool useRepository = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get STP payments by a list of Transaction IDs.
        /// </summary>
        /// <param name="transactionIds">The list of transaction Ids of the STP payments.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The list of STP payments for the Transaction Ids if records exist; Empty list otherwise.</returns>
        Task<IEnumerable<StpPayment>> GetStpPayments(IEnumerable<long> transactionIds, bool useRepository = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get list of STP Payments that match the search criteria for <see cref="StpPaymentFilter"/> class.
        /// </summary>
        /// <param name="networkName">Filter by network partner name. Null for all networks.</param>
        /// <param name="enrollType">Filter by enrollment type: B2B or C2B. Null for all enrollment types.</param>
        /// <param name="edi835Type">The type of EDI 835 to filter by: PRS or SDS. Null for all EDI 835 types.</param>
        /// <param name="edi835Avail">Filter for STP payments that have an available EDI 835. Null to ignore flag.</param>
        /// <param name="remitValid">Filter for STP payments for which the Remit has been marked as valid. Null to ignore flag.</param>
        /// <param name="paymentSent">Filter for STP payments that have been marked as sent. Null to ignore flag.</param>
        /// <param name="pmbMessageSent">Filter for STP payments that have had the PMB message sent. Null to ignore flag.</param>
        /// <param name="paymentSentRetries">Filter for STP payments that have a given number of payment send retries. Null for any retry count.</param>
        /// <param name="maxResults">The number of max records to return. The default is 10,000.</param>
        /// <param name="cancellationToken">The token to cancel the request.</param>
        /// <returns>The list of STP payments that match the requested criteria if any; Empty list otherwise.</returns>
        Task<IList<StpPayment>> SearchStpPayments(string networkName = null, string enrollType = null, string edi835Type = null, bool? edi835Avail = null, bool? remitValid = null, bool? paymentSent = null, bool? pmbMessageSent = null, int? paymentSentRetries = null, int maxResults = 10000, CancellationToken cancellationToken = default);

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
        /// <param name="pmbMessageSent">Indicates if the PMB message has been sent. The default is false.</param>
        /// <exception cref="DataException"></exception>
        Task<int> InsertStpPayments(IEnumerable<long> transactionIds, string networkName, string enrollType, string edi835Type, bool remitValid = false, bool edi835Avail = false, bool paymentSent = false, bool pmbMessageSent = false, CancellationToken cancellationToken = default);

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
    }
}
