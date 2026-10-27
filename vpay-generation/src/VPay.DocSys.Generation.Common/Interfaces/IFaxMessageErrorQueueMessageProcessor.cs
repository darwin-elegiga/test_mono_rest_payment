namespace VPay.DocSys.Generation.Common
{
    /// <summary>
    /// This is the processor for the fax message error queue
    /// </summary>
    public interface IFaxMessageErrorQueueMessageProcessor
    {
        /// <summary>
        /// Process the message string by republishing the message until the max retry count has been reached
        /// </summary>
        /// <param name="message"></param>
        /// <param name="maxRetryCount">retry count</param>
        /// <returns></returns>
        void ProcessMessageString(string message, int maxRetryCount);
    }
}
