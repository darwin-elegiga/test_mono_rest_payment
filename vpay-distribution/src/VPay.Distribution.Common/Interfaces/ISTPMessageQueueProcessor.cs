using System.Threading;
using System.Threading.Tasks;
using VPay.DocSys.Common;

namespace VPay.Distribution.Common
{
    public interface ISTPMessageQueueProcessor
    {
        /// <summary>
        /// Process the <paramref name="message"/> for distribution 
        /// </summary>
        /// <param name="message">A message that contains the information about the STP Remit messages to distribute.</param>
        Task<DistributionProcessResult> ProcessMessage(DocDistMsg message, CancellationToken cancellationToken = default);
    }
}
