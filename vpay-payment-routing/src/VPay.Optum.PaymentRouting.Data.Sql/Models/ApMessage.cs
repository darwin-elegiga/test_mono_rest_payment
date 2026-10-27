using System;

namespace VPay.Optum.PaymentRouting.Data.Sql.Models
{
    public class ApMessage
    {
        public long Id { get; set; }
        public long TransactionId { get; set; }
        public string ClientCode { get; set; } = string.Empty;
        public string BillingEntityCode { get; set; } = string.Empty;
        public string ApMessageType { get; set; } = string.Empty;
        public string ApRoutingType { get; set; } = string.Empty;
        public bool IsOptumEnrolled { get; set; }
        // Avro Fields
        public string PayerId { get; set; } = string.Empty;
        public string SubPayerId { get; set; } = string.Empty;
        public string PaymentSource { get; set; } = string.Empty;
        public string PaymentType { get; set; } = string.Empty;
        public string ProviderNumber { get; set; } = string.Empty;
        public string ProviderName { get; set; } = string.Empty;
        public string ProviderNpiNumber { get; set; } = string.Empty;
        public string UnConsolPayNumber { get; set; } = string.Empty;
        public string UnConsolPayAmount { get; set; } = string.Empty;
        public string DisplayConsolPayNumber { get; set; } = string.Empty;
        public string ConsolAmount { get; set; } = string.Empty;
        public string PayMethodCode { get; set; } = string.Empty;
        public string SettlementDate { get; set; } = string.Empty;
        public string CheckNumber { get; set; } = string.Empty;
        public string ReturnReasonCode { get; set; } = string.Empty;
        public string RevisedMethodCode { get; set; } = string.Empty;
        public string ReOriginatedDate { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedDateTime { get; set; }
        public string ModifiedBy { get; set; } = string.Empty;
        public DateTime? ModifiedDateTime { get; set; }
    }
}
