using Newtonsoft.Json;

namespace VPay.Gateway.Common.PlatformConfig;

public class ClientConfigurationDto
{
    [JsonProperty]
    public string OriginCode { get; set; } = default!;

    [JsonProperty]
    public string ClientCode { get; set; } = default!;

    [JsonProperty]
    public string BillingEntityCode { get; set; } = default!;

    [JsonProperty]
    public string Status { get; set; } = default!;

    [JsonProperty]
    public string Data1 { get; set; } = default!;

    [JsonProperty]
    public string Data2 { get; set; } = default!;
}
