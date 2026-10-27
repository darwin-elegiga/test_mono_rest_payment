using System;
using Newtonsoft.Json;

namespace VPay.Optum.Http.ConfigApi.Models
{
    public class OtbBillingEntityFieldDto
    {
        public string FieldName { get; set; } = string.Empty;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string CommonName { get; set; } = null;
        public string FieldValue { get; set; } = string.Empty;
        public int FieldOrder { get; set; }
        public DateTime EffectiveFromDate { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? EffectiveToDate { get; set; }
    }

}
