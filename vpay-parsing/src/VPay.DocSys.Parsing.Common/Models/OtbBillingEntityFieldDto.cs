using Newtonsoft.Json;
using System;

namespace VPay.DocSys.Parsing.Common.Models
{
    public class OtbBillingEntityFieldDto
    {
        public string FieldName { get; set; } = string.Empty;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string CommonName { get; set; } = string.Empty;
        public string FieldValue { get; set; } = string.Empty;
        public int FieldOrder { get; set; }
        public DateTime EffectiveFromDate { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? EffectiveToDate { get; set; }
    }
}
