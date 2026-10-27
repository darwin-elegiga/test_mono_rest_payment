using Newtonsoft.Json;

namespace VPay.Optum.Http.ConfigApi.Models
{
    public class ConfigurationDto
    {
        [JsonProperty]
        public string OriginCode { get; set; }

        [JsonProperty]
        public string ClientCode { get; set; }

        [JsonProperty]
        public string BillingEntityCode { get; set; }

        [JsonProperty]
        public string Status { get; set; }

        [JsonProperty]
        public string Data1 { get; set; }

        [JsonProperty]
        public string Data2 { get; set; }
    }
}
