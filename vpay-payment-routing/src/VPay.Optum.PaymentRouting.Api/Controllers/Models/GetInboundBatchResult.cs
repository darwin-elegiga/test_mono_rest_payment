using System;
using System.Collections.Generic;

namespace VPay.Optum.PaymentRouting.Api.Controllers.Models
{
    public class GetInboundBatchResult
    {
        public long InboundBatchId { get; init; }
        public long InboundHeaderId { get; init; }
        public string OptumBatchId { get; init; } = string.Empty;
        public string Payer835Id { get; init; } = string.Empty;
        public string PayerSubpayerId { get; init; } = string.Empty;
        public string PayerAlias { get; init; } = string.Empty;
        public int TotalPaymentCount { get; init; }
        public decimal TotalPaymentAmount { get; init; }
        public DateTime BatchCreatedDateTime { get; init; }
        public DateTime HeaderCreatedDateTime { get; init; }
        public string HeaderData { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public IEnumerable<long> InboundPaymentIds { get; set; } = Array.Empty<long>();
    }
}
