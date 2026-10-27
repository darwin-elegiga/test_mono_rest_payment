using System.Collections.Generic;
using System.Threading.Tasks;

using VPay.Data.Db2.Abstractions.Fax;

namespace VPay.DocSys.Generation.Common
{
    /// <summary>
    /// Payor Detail Resolver
    /// </summary>
    public interface IPayorDetailResolver
    {
        /// <summary>
        /// Returns the <see cref="PayorDetail"/> for the given parameters
        /// </summary>
        /// <param name="clientType"></param>
        /// <param name="tpaCode"></param>
        /// <param name="beCode"></param>
        /// <param name="transactionId"></param>
        /// <param name="otbDetailResponse"></param>
        /// <param name="configurationDictionary"></param>
        Task<PayorDetail> ResolvePayorDetails(
            ClientType clientType,
            string tpaCode,
            string beCode,
            long transactionId,
            OtbDetailResponse otbDetailResponse,
            FieldDictionary configurationDictionary);

        /// <summary>
        /// Creates a <see cref="FieldDictionary"/> from <paramref name="otbDetail"/> and <paramref name="vpaygeSettings"/>
        /// </summary>
        /// <param name="otbDetail"></param>
        /// <param name="vpaygeSettings"></param>
        /// <returns></returns>
        FieldDictionary CreateFieldDictionary(OtbDetailResponse otbDetail, ClientVPaygeSettingsResponse vpaygeSettings);
    }
}
