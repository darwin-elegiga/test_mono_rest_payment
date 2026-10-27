using System.Collections.Generic;
using System.Threading.Tasks;
using VPay.DocSys.Common;

namespace VPay.DocSys.Parsing.Common.Interfaces
{
    /// <summary>
    /// Service to return <see cref="OtbRecords"/>
    /// </summary>
    public interface IOtbService
    {
        /// <summary>
        /// Returns <see cref="OtbRecords"/> using the <paramref name="clientCode"/> and <paramref name="beCodes"/>
        /// </summary>
        /// <param name="clientCode">client code</param>
        /// <param name="beCodes">billing entity code</param>
        /// <returns></returns>
        Task<OtbRecords> GetOtbRecordsAsync(string clientCode, string[] beCodes);
    }
}
