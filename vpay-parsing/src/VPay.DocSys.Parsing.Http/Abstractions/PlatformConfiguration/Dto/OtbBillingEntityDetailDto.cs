using System.Collections.Generic;
using System.Linq;

namespace VPay.DocSys.Parsing.Http.Abstractions.PlatformConfiguration.Dto
{
    public class OtbBillingEntityDetailDto
    {
        public string BillingEntityCode { get; set; } = string.Empty;
        public IEnumerable<OtbBillingEntityFieldDto> Fields { get; set; } = Enumerable.Empty<OtbBillingEntityFieldDto>();
    }
}
