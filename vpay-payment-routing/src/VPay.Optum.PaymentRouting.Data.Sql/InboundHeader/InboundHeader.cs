using System;
using VPay.Optum.PaymentRouting.Data.SmartEnums;

namespace VPay.Optum.PaymentRouting.Data.Sql.InboundHeader
{
    public class InboundHeader
    {
        public long InboundBatchId { get; set; }
        public long InboundHeaderId { get; set; }
        public string OptumBatchId { get; set; }
        public string Payer835Id { get; set; }
        public string PayerSubpayerId { get; set; }
        public string PayerAlias { get; set; }
        public int TotalPaymentCount { get; set; }
        public decimal TotalPaymentAmount { get; set; }
        public DateTime BatchCreatedDateTime { get; set; }
        public DateTime HeaderCreatedDateTime { get; set; }
        public string HeaderData { get; set; }
        public PaymentStatus Status { get; set; }
        public DateTime LastModfiedDateTime { get; set; }
        public string LastModifiedBy { get; set; }

    }
}
