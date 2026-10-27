using Newtonsoft.Json;

namespace VPay.Gateway.Common.Recon.Models;

public class ReconPreTransactionDto
{
    public string? ClaimTransactionId { get; set; }

    public List<string>? ClaimIds { get; set; }

    public string? ClientReferenceId { get; set; }

    public double? Amount { get; set; }

    public string? Currency { get; set; }

    [JsonConverter(typeof(UTCDateTimeConverter))]
    public DateTime? CreatedTime { get; set; }

    [JsonConverter(typeof(UTCDateTimeConverter))]
    public DateTime? TimeOut { get; set; }

    public List<ReconPreTransactionStatusDto>? Statuses { get; set; }
}
