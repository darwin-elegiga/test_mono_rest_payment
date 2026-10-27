using Newtonsoft.Json;

namespace VPay.Gateway.Common.PepClaims.Models;

public class PepServiceParam
{
    [JsonProperty("origin")]
    public string OriginCode { get; set; } = "";

    [JsonProperty("tpaCode")]
    public string ClientCode { get; set; } = "";

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }
}
