using System;
using System.Collections.Generic;
using VPay.Optum.PaymentRouting.Data.Abstractions;

namespace VPay.Optum.PaymentRouting.Data.Sql.BatchManifest
{
    public class BatchManifest
    {
        /* First Resultset */
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
        public BatchStatus Status { get; set; }

        /* Second Resultset*/
        public List<long> InboundPaymentIds { get; set; }
    }
}
