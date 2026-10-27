using Ardalis.SmartEnum;

namespace VPay.Optum.PaymentRouting.Data.Abstractions
{
    public sealed class BatchStatus : SmartEnum<BatchStatus>
    {

        /// <summary>
        /// When we first "see" a OptumBatchId from Optum Inbound, we need to set the status history to this status.
        /// </summary>
        public static readonly BatchStatus BatchInitiated = new BatchStatus("BatchInitiated", 1);

        /// <summary>
        /// When all the components of a batch are gathered, we consider the batch received, and can queue
        /// it up for the next step in processing.
        /// </summary>
        public static readonly BatchStatus BatchReceived = new BatchStatus("BatchReceived", 2);

        /// <summary>
        /// After we've sent the batch for processing, upon successful completion of that process, it can
        /// be marked as "Complete", so that alerting/monitoring/reporting can indicate we've handled the
        /// inbound messages.
        /// </summary>
        public static readonly BatchStatus BatchComplete = new BatchStatus("BatchComplete", 3);

        /// <summary>
        /// If there is some error, or we've had some issue where the optumBatchId in question will not be
        /// completely received, we can set it to this status so any alerting/monitoring/reporting can
        /// exclude this value.
        /// </summary>
        public static readonly BatchStatus BatchCancelled = new BatchStatus("BatchCancelled", 99);


        private BatchStatus(string name, int value) : base(name, value) { }
    }
}
