using System.Threading;
using System.Threading.Tasks;

using VPay.DocSys.Common;

namespace VPay.Distribution.Common
{
    /// <summary>
    /// Distribution Queue Message Processor
    /// </summary>
    public interface IDistributionQueueMessageProcessor
    {
        /// <summary>
        /// Process the <paramref name="message"/> for distribution to the correct channels
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        Task<bool> ProcessMessage(DocDistMsg message, CancellationToken cancellationToken = default);
    }
}
