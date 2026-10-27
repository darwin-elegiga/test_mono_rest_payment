using System;
using Newtonsoft.Json;

namespace VPay.Optum.Http.ConfigApi.Models
{
    public class PayerPraConfigDto
    {
        public int ConfigurationId { get; set; }
        public string ConfigurationName { get; set; }
        public string OriginCode { get; set; }
        public string ClientCode { get; set; }
        public bool IsPayerPraEnabled { get; set; }
        public string RemitMethod { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? BeginDate { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? EndDate { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public string CreatedByProgram { get; set; }
        public string CreatedBy { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? LastModifiedDateTime { get; set; }
        public string LastModifiedByProgram { get; set; }
        public string LastModifiedBy { get; set; }
    }


}
