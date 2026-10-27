using System.Threading;
using System.Threading.Tasks;

using VPay.Distribution.Common.Models;
using VPay.DocSys.Common;

namespace VPay.Distribution.Common
{
    /// <summary>
    /// Event Message Publisher
    /// </summary>
    public interface IEdi835MessagePublisher
    {
        /// <summary>
        /// For each <see cref="DocDistMsg.Transactions"/> in <paramref name="docDistMsg"/> with a true
        /// <see cref="DistTxnData.IsEdi835Needed"/> flag, retrieve the <see cref="Edi835CreationMessage"/>
        /// from the database and publish to the Edi835Creation queue for generation.
        /// </summary>
        /// <remarks>
        /// Will validate status of <see cref="DocumentTransactionStatus.FinalDistType"/> for each transaction before queuing an <see cref="EventPublishQueueMessage"/>
        /// </remarks>
        /// <param name="docDistMsg"></param>
        /// <param name="cancellationToken"></param>
        /// <returns><see cref="Edi835PublishResponse"/></returns>
        Task<Edi835PublishResponse> PublishEdi835Messages(DocDistMsg docDistMsg, CancellationToken cancellationToken);

    }
}
