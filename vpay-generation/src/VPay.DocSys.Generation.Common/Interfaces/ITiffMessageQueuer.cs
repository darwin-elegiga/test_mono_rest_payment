using VPay.DocSys.Common;

namespace VPay.DocSys.Generation.Common
{
    /// <summary>
    /// This is the queuer for all <see cref="TiffQueueMessage"/>s
    /// </summary>
    public interface ITiffMessageQueuer
    {
        /// <summary>
        /// Queue the <see cref="TiffQueueMessage"/>
        /// </summary>
        /// <param name="message"></param>
        void QueueTiffMessage(TiffQueueMessage message);
    }
}
