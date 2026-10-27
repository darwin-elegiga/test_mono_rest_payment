using Newtonsoft.Json;

namespace VPay.Gateway.Common.Recon.Models;

public class ReconPreTransactionPartyDto
{
    public string? MemberId { get; set; }

    public string? PartyEmail { get; set; }

    public string? Phone1 { get; set; }

    public string? PhoneType1 { get; set; }

    public string? Phone2 { get; set; }

    public string? PhoneType2 { get; set; }

    public string? PartyName { get; set; }

    [JsonConverter(typeof(UTCDateTimeConverter))]
    public DateTime? CreatedTime { get; set; }

    public List<ReconPreTransactionStatusDto>? Statuses { get; set; }
}
