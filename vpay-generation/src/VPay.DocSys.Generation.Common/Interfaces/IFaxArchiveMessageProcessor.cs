using System.Threading.Tasks;

namespace VPay.DocSys.Generation.Common
{
    /// <summary>
    /// This is the message processor for archiving fax retries
    /// </summary>
    public interface IFaxArchiveMessageProcessor
    {
        /// <summary>
        /// Process the <see cref="FaxRetryArchiveMessage"/>
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        Task ProcessMessage(FaxRetryArchiveMessage faxRetryArchiveMessage);
    }
}
