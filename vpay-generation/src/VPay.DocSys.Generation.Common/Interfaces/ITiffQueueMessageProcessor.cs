using System.Threading.Tasks;

namespace VPay.DocSys.Generation.Common
{
    /// <summary>
    /// This is the Message Processor for the Tiff Queue
    /// </summary>
    public interface ITiffQueueMessageProcessor
    {
        /// <summary>
        /// Process the message string
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        Task ProcessMessageString(string message);
    }
}
