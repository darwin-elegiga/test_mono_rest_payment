using VPay.Optum.PaymentRouting.Data.SmartEnums;

namespace VPay.Optum.PaymentRouting.Data.Sql.Models
{
    public record Outbound835PaymentDetails
    {
        public long Id { get; set; } = default;
        public PaymentStatus PaymentStatus { get; set; } = default!;
        public string PaymentTypeCode { get; set; } = default!;
        public ApMessageTrackingStatus ApMessageTrackingStatus { get; set; } = default!;
    }
}
