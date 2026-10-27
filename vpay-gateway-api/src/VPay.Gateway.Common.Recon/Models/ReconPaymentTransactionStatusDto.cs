using Newtonsoft.Json;

namespace VPay.Gateway.Common.Recon.Models;

public class ReconPaymentTransactionStatusDto
{
    public string? Status { get; set; }

    [JsonConverter(typeof(UTCDateTimeConverter))]
    public DateTime? ProcessedTime { get; set; }

    public string? ReasonCode { get; set; }

    [JsonConverter(typeof(UTCDateTimeConverter))]
    public DateTime? TransactionTime { get; set; }

    public double? Amount { get; set; }

    public string? Currency { get; set; }

    public string? PayeeName { get; set; }

    public string? DistributionClass { get; set; }

    public string? DistributionTrackingNumber { get; set; }

    public string? AchId { get; set; }
}
