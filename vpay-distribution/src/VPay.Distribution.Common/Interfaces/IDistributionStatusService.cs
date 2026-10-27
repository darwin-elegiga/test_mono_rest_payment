using System.Threading.Tasks;

using VPay.Distribution.Common.Models;
using VPay.DocSys.Data;

namespace VPay.Distribution.Common
{
    /// <summary>
    /// Provides methods to manage the distribution status of file and transactions.
    /// </summary>
    public interface IDistributionStatusService
    {
        Task SetDistributionStatusComplete(string statusFilePath, string distType);
        Task<ClearDistributionStatusResults> ClearDistributionStatus(string statusFilePath, DistributionMethod distType);
    }
}
