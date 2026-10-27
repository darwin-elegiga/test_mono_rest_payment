using System.Threading.Tasks;

namespace VPay.DocSys.Generation.Common
{
    /// <summary>
    /// Platform Configuration Api Client
    /// </summary>
    public interface IPlatformConfigurationApiClient
    {
        /// <summary>
        /// Gets OTB Details for <paramref name="tpaCode"/> and <paramref name="beCode"/>
        /// </summary>
        /// <param name="tpaCode"></param>
        /// <param name="beCode"></param>
        /// <returns><see cref="OtbDetailResponse"/></returns>
        Task<OtbDetailResponse> GetOtbDetail(string tpaCode, string beCode);
    }
}
