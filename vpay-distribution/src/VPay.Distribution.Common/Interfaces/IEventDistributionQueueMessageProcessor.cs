using System.Threading;
using System.Threading.Tasks;

namespace VPay.Distribution.Common
{
    /// <summary>
    /// Event Distribution Queue Message Processor
    /// </summary>
    public interface IEventDistributionQueueMessageProcessor
    {
        /// <summary>
        /// Process the <paramref name="message"/> for distribution to the event bus
        /// </summary>
        /// <param name="message">A message that contains the information about the transactions to distribute.</param>
        Task ProcessMessage(EventPublishQueueMessage message, CancellationToken cancellationToken = default);
    }
}
