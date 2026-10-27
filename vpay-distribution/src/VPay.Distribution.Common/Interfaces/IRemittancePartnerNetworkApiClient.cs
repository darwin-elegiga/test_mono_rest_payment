using System.Threading;
using System.Threading.Tasks;

namespace VPay.Distribution.Common
{
    public interface IRemittancePartnerNetworkApiClient
    {
        /// <summary>
        /// Validate the Straight Thru Processing remittance document via Remittance Partner Network API.
        /// </summary>
        /// <param name="enrollmentType">Straight Thru Processing network enrollment type.</param>
        /// <param name="remittanceData">Straight Thru Processing remittance document data.</param>
        /// <returns></returns>
        /// <param name="cancellationToken"></param>
        Task<(bool documentAccepted, string responseBody)> ValidateRemittanceDocument(
                RemittanceNetworkEnrollmentTypes enrollmentType,
                string remittanceData,
                CancellationToken cancellationToken = default);

        /// <summary>
        /// Send the Straight Thru Processing remittance document to Remittance Partner Network API.
        /// </summary>
        /// <param name="enrollmentType">Straight Thru Processing network enrollment type.</param>
        /// <param name="remittanceData">Straight Thru Processing remittance document data.</param>
        /// <returns></returns>
        /// <param name="cancellationToken"></param>
        Task<(bool documentAccepted, string responseBody)> PostRemittanceDocument(
            RemittanceNetworkEnrollmentTypes enrollmentType,
            string remittanceData,
            CancellationToken cancellationToken = default);
    }
}
