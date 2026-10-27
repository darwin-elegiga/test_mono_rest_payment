using System.Threading;
using System.Threading.Tasks;

using VPay.DocSys.Generation.Common.Models;
using VPay.DocSys.Common;

namespace VPay.DocSys.Generation.Common
{
    /// <summary>
    /// Event Message Publisher
    /// </summary>
    public interface IEdi835MessagePublisher
    {
        /// <summary>
        /// For a transaction that exists with an Edi835 record, will publish the message to be generated for sending
        /// to SDS.
        /// </summary>
        Task PublishEdi835Message(long transactionId, CancellationToken cancellationToken);

    }
}
