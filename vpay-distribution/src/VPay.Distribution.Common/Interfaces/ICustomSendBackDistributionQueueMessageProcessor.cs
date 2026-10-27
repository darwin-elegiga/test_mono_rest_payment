using System.Threading;
using System.Threading.Tasks;

using VPay.DocSys.Common;

namespace VPay.Distribution.Common
{
    /// <summary>
    /// Custom Send Back Distribution Queue Message Processor
    /// </summary>
    public interface ICustomSendBackDistributionQueueMessageProcessor
    {
        /// <summary>
        /// Process the <paramref name="message"/> for distribution to FSSI.
        /// </summary>
        /// <param name="message">A message that contains the information about the transactions to distribute.</param>
        Task ProcessMessage(DocDistMsg message, CancellationToken cancellationToken = default);
    }
}
