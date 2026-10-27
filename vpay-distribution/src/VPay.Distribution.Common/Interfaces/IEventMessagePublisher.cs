using System.Threading;
using System.Threading.Tasks;

using VPay.DocSys.Common;

namespace VPay.Distribution.Common
{
    /// <summary>
    /// Event Message Publisher
    /// </summary>
    public interface IEventMessagePublisher
    {
        /// <summary>
        /// Distribute by publishing an event for each of the given <see cref="DocDistMsg.Transactions"/> in <paramref name="docDistMsg"/>
        /// </summary>
        /// <remarks>
        /// Will validate status of <see cref="DocumentTransactionStatus.FinalDistType"/> for each transaction before queuing an <see cref="EventPublishQueueMessage"/>
        /// </remarks>
        /// <param name="docDistMsg"></param>
        /// <param name="cancellationToken"></param>
        /// <returns><see cref="EventPublishResponse"/></returns>
        Task<EventPublishResponse> ValidateAndPublishEventsForTransactionIds(DocDistMsg docDistMsg, CancellationToken cancellationToken);

        /// <summary>
        /// Distribute by publishing an event for each of the given <see cref="DocDistMsg.Transactions"/> in <paramref name="docDistMsg"/>
        /// </summary>
        /// <remarks>
        /// No validation of <see cref="DocumentTransactionStatus.FinalDistType"/> for each transaction
        /// </remarks>
        /// <param name="docDistMsg"></param>
        /// <returns></returns>
        void PublishEvents(DocDistMsg docDistMsg);
    }
}
