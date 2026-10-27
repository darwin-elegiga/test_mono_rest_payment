using System;
using System.Collections.Generic;
using VPay.DocSys.Data;

namespace VPay.DocSys.Parsing.Common
{
    public class StpPaymentDto
    {
        /// <summary>
        /// The transaction ID associated with the payment.
        /// </summary>
        public long TransactionId { get; set; }

        /// <summary>
        /// The network name associated with the payment.
        /// </summary>
        public string? NetworkName { get; set; }

        /// <summary>
        /// The network enrollment type: B2B or C2B.
        /// </summary>
        public string? EnrollType { get; set; }

        /// <summary>
        /// The EDI 835 type: PRS or SDS.
        /// </summary>
        public string? Edi835Type { get; set; }

        /// <summary>
        /// The flag indicating whether the remittance is valid.
        /// </summary>
        public bool RemitValid { get; set; }

        /// <summary>
        /// The flag indicating whether an EDI 835 file is available.
        /// </summary>
        public bool Edi835Avail { get; set; }

        /// <summary>
        /// The flag indicating whether the payment has been sent.
        /// </summary>
        public bool PaymentSent { get; set; }

        /// <summary>
        /// The flag indicating whether the PMB message has been sent.
        /// </summary>
        public bool PmbMessageSent { get; set; }

        /// <summary>
        /// The date and time when the remittance was validated.
        /// </summary>
        public DateTime? RemitValidDateTime { get; set; }

        /// <summary>
        /// The date and time when the EDI 835 file became available.
        /// </summary>
        public DateTime? Edi835AvailDateTime { get; set; }

        /// <summary>
        /// The date and time when the payment was sent.
        /// </summary>
        public DateTime? PaymentSentDateTime { get; set; }

        /// <summary>
        /// The date and time when the PMB message was sent.
        /// </summary>
        public DateTime? PmbMessageSentDateTime { get; set; }

        /// <summary>
        /// The number of retry attempts made when sending a payment fails.
        /// </summary>
        /// <remarks>This property determines how many times the system has attempted to resend a payment 
        /// if the initial attempt failed. A value of 0 indicates that no retries have been performed.</remarks>
        public int PaymentSentRetries { get; set; }

        /// <summary>
        /// The user who created the record.
        /// </summary>
        public string? CreatedUser { get; set; }

        /// <summary>
        /// The date and time when the record was created.
        /// </summary>
        public DateTime CreatedDateTime { get; set; }

        /// <summary>
        /// The user who last modified the record.
        /// </summary>
        public string? LastModifiedUser { get; set; }

        /// <summary>
        /// The date and time when the record was last modified.
        /// </summary>
        public DateTime LastModifiedDateTime { get; set; }

        public static StpPaymentDto Create(StpPayment stp)
        {
            return new StpPaymentDto
            {
                TransactionId = stp.TransactionId,
                NetworkName = stp.NetworkName,
                EnrollType = stp.EnrollType,
                Edi835Type = stp.Edi835Type,
                RemitValid = stp.RemitValid,
                Edi835Avail = stp.Edi835Avail,
                PaymentSent = stp.PaymentSent,
                RemitValidDateTime = stp.RemitValidDateTime,
                Edi835AvailDateTime = stp.Edi835AvailDateTime,
                PaymentSentDateTime = stp.PaymentSentDateTime,
                PaymentSentRetries = stp.PaymentSentRetries,
                CreatedUser = stp.CreatedUser,
                CreatedDateTime = stp.CreatedDateTime,
                LastModifiedUser = stp.LastModifiedUser,
                LastModifiedDateTime = stp.LastModifiedDateTime
            };
        }

        public static IEnumerable<StpPaymentDto> CreateList(IEnumerable<StpPayment> stpPayments)
        {
            foreach (var stp in stpPayments)
            {
                yield return Create(stp);
            }
        }
    }
}
