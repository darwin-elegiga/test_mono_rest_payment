using Ardalis.SmartEnum;

namespace VPay.Optum.PaymentRouting.Data.SmartEnums
{
    public sealed class ApMessageTrackingStatus : SmartEnum<ApMessageTrackingStatus>
    {

        /// <summary>
        /// When an ApMessageTracking is first inserted, it's inserted as "queued" it doesn't make any sense, since
        /// the thing inserting the message is reading the ApReapMessage FROM the queue.  But it is what it is.
        /// </summary>
        public static readonly ApMessageTrackingStatus Queued = new ApMessageTrackingStatus("QUEUED", 1);

        /// <summary>
        /// Indicates that we've sent an Avro message to Optum to let them know that we've processed the payment
        /// done whatever funding is necessary, assigned it a TransactionId and moving on to the next step
        /// </summary>
        public static readonly ApMessageTrackingStatus ApSent = new ApMessageTrackingStatus("APSENT", 2);

        /// <summary>
        /// When the ApMessageTracking is not OPACH nor CLCFT, we start the "835 build" which puts it into
        /// the Outbound835 queue.  The consumer of that will modify the 835 ST/SE segment for that
        /// payment to indicate that VPay handled it.
        /// </summary>
        public static readonly ApMessageTrackingStatus Started = new ApMessageTrackingStatus("START835BUILD", 3);


        /// <summary>
        /// When the ApMessageTracking is CLCFT, indicated that only the FinSeq is needed (no need to send
        /// AP message to Optum). Also, there's no logical reason the statuses must be all caps...
        /// </summary>
        public static readonly ApMessageTrackingStatus FinSegNeeded = new ApMessageTrackingStatus("FinSegNeeded", 4);

        /// <summary>
        /// When the ApMessageTracking is CLCFT, we start the "Fin Seg" which puts it into
        /// the OutboundFinSeg queue.  The consumer of that will copy the existing 835 ST/SE segment from
        /// dbo.ClobStorage to dbo.OutboundPaymentClob to be sent back to Optum
        /// This status will not be funded at VPay (CLCFT=Client Funding, which in this case is Optum Bank)
        /// Transfering the money to MetaBank to load, then back to Optum bank incurs fees that we
        /// are trying to avoid.
        /// </summary>
        public static readonly ApMessageTrackingStatus StartFinSegBuild = new ApMessageTrackingStatus("StartFinSegBuild", 5);

        /// <summary>
        /// Once we've completed the process (either 835 or FinSeg).  This is not the "end" of the payment, just
        /// the point at which we're indicating that we have done all we are going to do with the 835 ST/SE segment for
        /// it.
        /// </summary>
        public static readonly ApMessageTrackingStatus Completed = new ApMessageTrackingStatus("COMPLETED", 6);


        private ApMessageTrackingStatus(string name, int value) : base(name, value) { }
    }
}
