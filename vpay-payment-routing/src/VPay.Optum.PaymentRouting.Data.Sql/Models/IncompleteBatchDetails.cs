using System;

namespace VPay.Optum.PaymentRouting.Data.Sql.Models
{
    public class IncompleteBatchDetails
    {
        public long InboundBatchId { get; set; }
        public string OptumBatchId { get; set; }
        public string Payer835Id { get; set; }
        public int TotalPaymentCount { get; set; }
        public int ReceivedPaymentCount { get; set; }
        public decimal TotalPaymentAmount { get; set; }
        public decimal ReceivedPaymentAmount { get; set; }
        public int HeaderCount { get; set; }
        public int RequiredRemittanceAdviceFiles { get; set; }
        public int ReceivedRemittanceAdviceFiles { get; set; }
        public string Status { get; set; }
        public string IncompleteBatchReason { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public DateTime LastModifiedDateTime { get; set; }
    }
}
