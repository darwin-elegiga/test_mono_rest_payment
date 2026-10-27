using System.Collections.Generic;
using System.Linq;

namespace VPay.Optum.Http.ConfigApi.Models
{
    public class OtbBillingEntityDetailDto
    {
        public string BillingEntityCode { get; set; } = string.Empty;
        public IEnumerable<OtbBillingEntityFieldDto> Fields { get; set; } = Enumerable.Empty<OtbBillingEntityFieldDto>();

    }
}
