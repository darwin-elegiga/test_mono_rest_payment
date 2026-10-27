using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using VPay.DocSys.Storage;

namespace VPay.Distribution.Common
{
    /// <summary>
    /// Storage Api Client
    /// </summary>
    public interface IStorageApiClient
    {
        /// <summary>
        /// Returns all the <see cref="IArchiveFile"/>s for the given <paramref name="transactionId"/>
        /// </summary>
        /// <param name="transactionId"></param>
        /// <returns></returns>
        Task<IEnumerable<IArchiveFile>> GetArchivesForTransactionId(long transactionId, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the pdf of the custom send back check for the specified <paramref name="transactionId"/>.
        /// </summary>
        /// <param name="transactionId">The transaction id of the payment to look up.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<byte[]> GetCustomSendBackCheck(long transactionId, CancellationToken cancellationToken);       
    }
}
