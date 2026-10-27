namespace VPay.DocSys.Generation.Common
{
    /// <summary>
    /// This is the processor for all <see cref="FaxMessagePayload"/>s that come in manually
    /// </summary>
    public interface IFaxMessagePayloadProcessor
    {
        /// <summary>
        /// Create and queues a <see cref="FaxMessageQueueMessage"/>
        /// </summary>
        /// <param name="payload">the payload request for the new fax message</param>
        /// <returns></returns>
        void ProcessPayload(FaxMessagePayloadRequest payload);
    }
}
