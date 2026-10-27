using System.Threading.Tasks;

namespace VPay.DocSys.Generation.Common
{
    /// <summary>
    /// Parsing Api Client
    /// </summary>
    public interface IParsingApiClient
    {
        /// <summary>
        /// Gets the configuration for the given <paramref name="tpaCode"/> and <paramref name="beCode"/>
        /// </summary>
        /// <param name="tpaCode"></param>
        /// <param name="beCode"></param>
        /// <returns><see cref="ClientVPaygeSettingsResponse"/></returns>
        Task<ClientVPaygeSettingsResponse> GetXmlConfiguration(string tpaCode, string beCode);
    }
}
