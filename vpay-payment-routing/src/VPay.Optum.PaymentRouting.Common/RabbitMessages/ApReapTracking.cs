namespace VPay.Optum.PaymentRouting.Common.RabbitMessages
{
    public record ApReapTracking
    {
        public string ClientCode { get; set; } = string.Empty;
        public string BillingEntityCode { get; set; } = string.Empty;
        public string ApMessageType { get; set; } = string.Empty;
        public string ApRoutingType { get; set; } = string.Empty;
        public bool IsOptumEnrolled { get; set; }
        public long InboundPaymentId { get; set; }
        public string InboundPaymentStatus { get; set; } = string.Empty;
        public long ApMessageTrackingId { get; set; }
        public string ApMessageTrackingStatus { get; set; } = string.Empty;
    }
}
