namespace VPay.DocSys.Generation.Common
{
    public abstract class BaseFaxMessagePayload
    {
        /// <summary>
        /// The number of retries when moving this message from the error queue
        /// back towards the normal processing queue
        /// </summary>
        public int ErrorRetryCount { get; set; }
    }
}
