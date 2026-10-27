using System.Threading;
using System.Threading.Tasks;

namespace VPay.Distribution.Common
{
    public interface IRemittanceService
    {
        /// <summary>
        /// Post the remittance document for the given STP payment via Remit Partner Network API.
        /// </summary>
        /// <param name="jsonPayload">The contents of the STP payment to post in the form of a JSON payload.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. The operation will terminate early if the token is canceled.</param>
        /// <returns>True if post was accepted; False otherwise.</returns>
        Task<bool> PostB2BRemitDocument(string jsonPayload, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validate the remittance document for the given STP payment via Remit Partner Network API.
        /// </summary>
        /// <param name="jsonPayload">The contents of the STP payment to post in the form of a JSON payload.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. The operation will terminate early if the token is canceled.</param>
        /// <returns>True if validation was successful; False otherwise.</returns>
        Task<bool> ValidateB2BRemitDocument(string jsonPayload, CancellationToken cancellationToken = default);

        /// <summary>
        /// Post the remittance document for the given STP payment via Remit Partner Network API.
        /// </summary>
        /// <param name="transactionId">The transaction Id of the STP payment to post.</param>
        /// <param name="enrollmentType">The enrollment type of the STP payment to post: B2B or C2B.</param>
        /// <param name="jsonPayload">The contents of the STP payment to post in the form of a JSON payload.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. The operation will terminate early if the token is canceled.</param>
        /// <returns>True if post was accepted; False otherwise.</returns>
        Task<bool> PostRemitDocument(long transactionId, string enrollmentType, string jsonPayload, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validate the remittance document for the given STP payment via Remit Partner Network API.
        /// </summary>
        /// <param name="transactionId">The transaction Id of the STP payment to post.</param>
        /// <param name="enrollmentType">The enrollment type of the STP payment to post: B2B or C2B.</param>
        /// <param name="jsonPayload">The contents of the STP payment to post in the form of a JSON payload.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. The operation will terminate early if the token is canceled.</param>
        /// <returns>True if validation was successful; False otherwise.</returns>
        Task<bool> ValidateRemitDocument(long transactionId, string enrollmentType, string jsonPayload, CancellationToken cancellationToken = default);
    }
}
