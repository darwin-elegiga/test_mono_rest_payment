using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VPay.DocSys.Storage;

namespace VPay.Distribution.Common
{
    public interface IStorageService
    {
        /// <summary>
        /// Get the list of ArchiveFiles from Document System Storage for the given transaction Id.
        /// </summary>
        /// <param name="transactionId">The transaction Id.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. The operation will terminate early if the token is canceled.</param>
        /// <returns>The list of ArchiveFiles.</returns>
        Task<IEnumerable<IArchiveFile>> GetArchiveFiles(long transactionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get the ArchiveDetail for the STP Remittance JSON file from Document System Storage for the given transaction Id.
        /// </summary>
        /// <param name="transactionId">The transaction id.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. The operation will terminate early if the token is canceled.</param>
        /// <returns>The ArchiveDetail for the STP Remittance JSON file.</returns>
        Task<IArchiveDetail> GetRemitJsonDetail(long transactionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get the contents of the STP Remittance JSON file from Document System Storage for the given transaction Id.
        /// </summary>
        /// <param name="transactionId">The transaction id.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. The operation will terminate early if the token is canceled.</param>
        /// <returns>The contents of the STP Remittance JSON file.</returns>
        Task<string> GetRemitJson(long transactionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Format the remittance JSON string by ensuring each key-value pair is on its own line.
        /// </summary>
        /// <param name="value">The raw JSON string.</param>
        /// <returns>The formatted JSON string.</returns>
        string FormatRemitJson(string value);

        /// <summary>
        /// Verify the existence of the SDS EDI 835 file from Document System Storage for the given transaction Id.
        /// </summary>
        /// <param name="transactionId">The transaction id.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. The operation will terminate early if the token is canceled.</param>
        /// <returns>The contents of the SDS EDI 835 file.</returns>
        Task<bool> CanDownloadEdi835(long transactionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get the contents of the SDS EDI 835 file from Document System Storage for the given transaction Id.
        /// </summary>
        /// <param name="transactionId">The transaction Id.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. The operation will terminate early if the token is canceled.</param>
        /// <returns>The contents of the SDS EDI 835 file.</returns>
        Task<string> DownloadEdi835(long transactionId, CancellationToken cancellationToken = default);
    }
}
