using Ardalis.SmartEnum;

namespace VPay.Optum.PaymentRouting.Data.SmartEnums
{
    /// <summary>
    /// Must comply with [CHK_InboundPayment_Status]
    /// </summary>
    public sealed class PaymentStatus : SmartEnum<PaymentStatus>
    {
        /// <summary>
        /// When we first "see" a payment from Optum Inbound, we need to set the status history to this status.
        /// </summary>
        public static readonly PaymentStatus Received = new("RECEIVED", 1);

        /// <summary>
        /// After we receive an AP/REAP message from iseries and publish a message to optum.
        /// This status will go to either 
        /// </summary>
        public static readonly PaymentStatus ApSent = new("APSENT", 2);

        /// <summary>
        /// After we receive an AP/REAP message from iseries and skip publishing a message to optum.
        /// This is a terminal status (indicates no further action was necessary)
        /// </summary>
        public static readonly PaymentStatus ApSentSkip = new("APSENTSKIP", 3);



        /// <summary>
        /// After we have queued the payment for 835 generation. (File _should_ have been built at this point)
        /// </summary>
        /// <remarks>This status is set by SQL STORED PROCEDURE [dbo].[usp_Outbound835File_Insert]
        /// but is otherwise not referenced in code.
        /// </remarks>
        public static readonly PaymentStatus Queued835Generation = new("QUEUED835GEN", 6);

        /// <summary>
        /// After we queue up CLCFT for FinSeg generation
        /// </summary>
        public static readonly PaymentStatus FinSegQueued = new("FinSegQueued", 7);




        /// <summary>
        /// After we have processed and sent out the 835 message.
        /// </summary>
        /// <remarks>After the payments are processed on the Outbound835 Queue (and have the dbo.OutboundPaymentClob updated for 835)
        /// This status is set.
        /// ActiveBatch calls the /api/Outbound835/start method with no parameters, and any dbo.InboundPayments in this
        /// status will be grouped by the OutboundRefEo value and put into the Outbound835File queue to generate
        /// files to send to Optum.  
        /// </remarks>
        public static readonly PaymentStatus Out835Built = new("OUT835BUILT", 4);



        /// <summary>
        /// After we have processed the OutboundFinSeg payments (and copied the inbound ClobStorage recrod to the OutboundPaymentClob table).
        /// I don't care for the name of this, but it's already baked in, and the level of effort to update all the code/db/documentation/etc...
        /// Oy vey
        /// </summary>
        /// <remarks>After the payments are processed on the Outbound835 Queue (and have the dbo.OutboundPaymentClob updated for 835)
        /// This status is set.
        /// ActiveBatch calls the /api/Outbound835/start method with no parameters, and any dbo.InboundPayments in this
        /// status will be grouped by the OutboundRefEo value and put into the Outbound835File queue to generate
        /// files to send to Optum.  
        /// </remarks>
        public static readonly PaymentStatus FinSegBuilt = new("FinSegBuilt", 5);






        /// <summary>
        /// If there is some error, or we've had some issue where we need to remove a payment
        /// we want to mark it as such for easier identification.
        /// </summary>
        public static readonly PaymentStatus Cancelled = new("CANCELLED", 99);

        private PaymentStatus(string name, int value) : base(name, value) { }
    }
}
