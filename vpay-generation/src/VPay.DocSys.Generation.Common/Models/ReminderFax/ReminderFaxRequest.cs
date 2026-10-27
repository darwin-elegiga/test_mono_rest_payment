using System;
using System.ComponentModel.DataAnnotations;

using VPay.Data.Db2.Abstractions.Fax;

namespace VPay.DocSys.Generation.Common
{
    public class ReminderFaxRequest
    {
        [Required]
        public string OriginCode { get; set; }

        [Required]
        public string TPACode { get; set; }

        [Required]
        public string BillingEntity { get; set; }

        public ClientType ClientType { get; set; }

        public ReminderFaxType ReminderType { get; set; }

        public long SeTransactionId { get; set; }

        public long CorrespondenceId { get; set; }

        public string PaymentTypeCode { get; set; }

        public decimal AvailableBalance { get; set; }

        [Required]
        public string UseFaxNumber { get; set; }

        [Required]
        public string PayeeName { get; set; }

        public DateTime LastFaxDate { get; set; }

        public ReminderFax ToReminderFax()
        {
            return new ReminderFax
            {
                AvailableBalance = AvailableBalance,
                BillingEntity = BillingEntity,
                ClientType = ClientType,
                CorrespondenceId = CorrespondenceId,
                LastFaxDate = LastFaxDate,
                OriginCode = OriginCode,
                PayeeName = PayeeName,
                PaymentTypeCode = PaymentTypeCode,
                ReminderType = ReminderType.ToString(),
                SeTransactionId = SeTransactionId,
                TPACode = TPACode,
                UseFaxNumber = UseFaxNumber,
            };
        }
    }
}
