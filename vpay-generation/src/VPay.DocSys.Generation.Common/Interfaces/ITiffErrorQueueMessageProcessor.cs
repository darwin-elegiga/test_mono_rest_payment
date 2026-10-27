using System.Threading.Tasks;

namespace VPay.DocSys.Generation.Common
{
    /// <summary>
    /// This is the processor for the tiff message error queue
    /// </summary>
    public interface ITiffErrorQueueMessageProcessor
    {
        /// <summary>
        /// Process the message string
        /// </summary>
        /// <param name="message"></param>
        /// <param name="maxRetryCount">retry count</param>
        /// <returns></returns>
        Task ProcessMessageString(string message, int maxRetryCount);
    }
}
