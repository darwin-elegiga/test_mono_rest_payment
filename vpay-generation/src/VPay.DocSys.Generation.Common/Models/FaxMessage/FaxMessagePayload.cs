using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace VPay.DocSys.Generation.Common
{
    public class FaxMessagePayload : BaseFaxMessagePayload
    {
        [Required]
        [JsonProperty("xml")]
        public string VPaygeXmlBase64 { get; set; }

        [Required]
        [JsonProperty("pay")]
        public string PaymentType { get; set; }

        [JsonProperty("cid")]
        public long CorrespondenceId { get; set; }

        [JsonProperty("tids")]
        public List<long> TransactionIds { get; set; }

        [Required]
        [JsonProperty("faxN")]
        public string FaxNumber { get; set; }

        [Required]
        [JsonProperty("ori")]
        public string Origin { get; set; }
    }
}
