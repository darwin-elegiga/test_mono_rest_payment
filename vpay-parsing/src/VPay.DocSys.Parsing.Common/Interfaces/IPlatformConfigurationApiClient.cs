using System.Collections.Generic;
using System.Threading.Tasks;
using VPay.DocSys.Parsing.Common.Models;

namespace VPay.DocSys.Parsing.Common
{
    /// <summary>
    /// Platform Configuration API Client
    /// </summary>
    public interface IPlatformConfigurationApiClient
    {
        /// <summary>
        /// Gets OTB data for Client <paramref name="tpaCode"/> including only the Billing Entities specified in the list.
        /// </summary>
        /// <param name="tpaCode">The client code.</param>
        /// <param name="beCodes">The list of billing entity codes to retrieve.</param>
        /// <returns><see cref="OtbBillingEntityDetailDto"/></returns>
        Task<OtbBillingEntityClientDto> GetOtbClientBillingEntities(string tpaCode, ICollection<string> beCodes);
    }
}
