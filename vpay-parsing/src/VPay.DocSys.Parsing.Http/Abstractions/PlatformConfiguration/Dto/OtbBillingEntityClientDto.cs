using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace VPay.DocSys.Parsing.Http.Abstractions.PlatformConfiguration.Dto
{
    public class OtbBillingEntityClientDto
    {
        public string ClientCode { get; set; } = string.Empty;

        public string OriginCode { get; set; } = string.Empty;

        public string OriginName { get; set; } = string.Empty;

        public IEnumerable<OtbBillingEntityDetailDto> BillingEntities { get; set; } = Enumerable.Empty<OtbBillingEntityDetailDto>();

        public DateTime EffectiveFromDate { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? EffectiveToDate { get; set; }
    }
}
