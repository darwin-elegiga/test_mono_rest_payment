using System.Threading.Tasks;

namespace VPay.DocSys.Generation.Common
{
    /// <summary>
    /// This is the Message Processor for the Fax Queue
    /// </summary>
    public interface IFaxMessageQueueMessageProcessor
    {
        /// <summary>
        /// Process the message string
        /// </summary>
        /// <param name="message">message string</param>
        /// <returns></returns>
        Task ProcessMessageString(string message);
    }
}
