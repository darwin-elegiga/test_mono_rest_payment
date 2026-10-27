using System;
using VPay.Optum.PaymentRouting.Data.SmartEnums;

namespace VPay.Optum.PaymentRouting.Data.Sql.Models
{
    public class InboundPayment
    {
        public long Id { get; set; }
        public long InboundBatchId { get; set; }
        public long ClobStorageId { get; set; }
        public string PaymentClob { get; set; }
        public string UserKey { get; set; }
        public string Payer835Id { get; set; }
        public long FileProcId { get; set; }
        public string PayerAlias { get; set; }
        public long ProvTin { get; set; }
        public string PayerSubpayerId { get; set; }
        public long ZipCode { get; set; }
        public decimal PaymentAmount { get; set; }
        public PaymentStatus Status { get; set; }
        public string OutboundRefEo { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public string LastModifiedBy { get; set; }
        public DateTime LastModifiedDateTime { get; set; }
    }
}
