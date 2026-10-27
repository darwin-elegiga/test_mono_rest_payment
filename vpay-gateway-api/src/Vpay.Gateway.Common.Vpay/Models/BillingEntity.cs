using Newtonsoft.Json;

namespace Vpay.Gateway.Common.Vpay.Models;

public class BillingEntity
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("code")]
    public string? Code { get; set; }

    public Tpa? Tpa { get; set; }
}

public class Tpa
{
    [JsonProperty("id")]
    public int Id { get; set; }
    [JsonProperty("code")]
    public string? Code { get; set; }
}
