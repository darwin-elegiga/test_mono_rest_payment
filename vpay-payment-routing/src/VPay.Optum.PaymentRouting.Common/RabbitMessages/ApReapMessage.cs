using System;
using Newtonsoft.Json;
using VPay.Optum.PaymentRouting.Common.Helpers;

namespace VPay.Optum.PaymentRouting.Common.RabbitMessages
{
    /// <summary>
    /// This should be the record from ISeries
    /// </summary>
    public record ApReapMessage
    {
        public string Origin { get; set; } = default!;

        public string TpaCode { get; set; } = default!;

        public string BillingEntity { get; set; } = default!;

        public string MessageType { get; set; } = default!;

        public decimal TransactionId { get; set; }

        public string ProviderTin { get; set; } = default!;

        public string ProviderName { get; set; } = default!;

        public string ProviderNPPID { get; set; } = default!;

        public string UserKey { get; set; } = default!;

        public decimal LoadAmount { get; set; }

        [JsonConverter(typeof(ISeriesDateTimeConverter)), JsonProperty("settlementdate")]
        public DateTime LoadDate { get; set; }

        public string PaymentType { get; set; } = default!;

        public string CheckNumber { get; set; } = default!;

        public string TrnSegment { get; set; } = default!;

        public string ReapCheckNumber { get; set; } = default!;

        public decimal? ReapTransactionId { get; set; }

        public string ReapReasonCode { get; set; } = default!;

        [JsonProperty("revisedmethodcode")]
        public string ReapPaymentType { get; set; } = default!;

        [JsonConverter(typeof(ISeriesDateTimeConverter)), JsonProperty("reoriginateddate")]
        public DateTime? ReapLoadDate { get; set; }
    }
}
