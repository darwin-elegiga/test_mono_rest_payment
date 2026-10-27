using System.Threading;
using System.Threading.Tasks;

using VPay.DocSys.Common;

namespace VPay.Distribution.Common
{
    /// <summary>
    /// Fax Distribution Queue Message Processor
    /// </summary>
    public interface IFaxDistributionQueueMessageProcessor
    {
        /// <summary>
        /// Process the <paramref name="message"/> for distribution to the fax system.
        /// </summary>
        /// <param name="message">A message that contains the information about the transactions to distribute.</param>
        Task ProcessMessage(DocDistMsg message, CancellationToken cancellationToken = default);
    }
}
