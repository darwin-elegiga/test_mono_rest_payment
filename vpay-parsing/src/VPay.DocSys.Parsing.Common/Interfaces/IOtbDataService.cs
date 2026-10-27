using System.Collections.Generic;
using System.Threading.Tasks;
using VPay.DocSys.Parsing.Common.Models;

namespace VPay.DocSys.Parsing.Common.Interfaces
{
    public interface IOtbDataService
    {
        /// <summary>
        /// The flag to determine if the OTB data should be extracted from the OTB API.
        /// </summary>
        bool UseApi { get; }

        /// <summary>
        /// The flag to determine if the OTB data should be extracted from the OTB File.
        /// </summary>
        bool UseFile { get; }

        /// <summary>
        /// Retrieve OTB client and billing entity data from Platform Configuration API.
        /// </summary>
        /// <param name="clientCode">The client code.</param>
        /// <param name="beCodes">The optional list of billing entity codes to retrieve
        /// or null or empty list to retrieve all billing entities.</param>
        /// <returns>The OTB client and billing entity data.</returns>
        Task<OtbBillingEntityClientData> GetOtbDataAsync(string clientCode, ICollection<string> beCodes = null);
    }
}
