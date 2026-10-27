using Ardalis.SmartEnum;

namespace VPay.Optum.PaymentRouting.Data.SmartEnums
{
    public sealed class Outbound835FileStatus : SmartEnum<Outbound835FileStatus>
    {
        /// <summary>
        /// Outbound835 are grouped by PayerId and OutboundRefEo and sent to Outbound835File queue
        /// to be processed.
        /// </summary>
        public static readonly Outbound835FileStatus Queued = new("QUEUED", 1);

        /// <summary>
        /// Same as <see cref="Queued"/>, but for the FinSeg file
        /// Indicates a message queued for generation of the FinSeg Txt.
        /// The "FINSEG TXT" with a filename like MUM25_835FINGSEG_20220222_01.txt
        /// and contains the metadata about the actual 835
        /// </summary>
        public static readonly Outbound835FileStatus FinSegTxtQueued = new("FinSegTxtQueued", 11);

        /// <summary>
        /// Same as <see cref="Queued"/>, but for the unaltered 835 file 
        /// Indicates a message queued for generation of the 835 associated with the FinSeg file
        /// Name will be like MUM25_20220222_01 with no file extension and contain the ISA/GS records
        /// all of the ST/SE segments for each payment and trailing GE/IEA records.
        /// </summary>
        public static readonly Outbound835FileStatus FinSeg835Queued = new("FinSeg835Queued", 21);


        /// <summary>
        /// CreateOutbound835File _should_ set this status... but I guess it wasn't done.
        /// There is a message in [dbo].[Outbound835File] properties descriptions indicating
        /// MessageQueueId _should_ be set when status is going to STARTED.
        ///
        /// However, in all production data, MessageQueueId is NULL.  This may be cruft
        /// from a rapid design/build process.
        ///
        /// No evidence this has ever been used (as of 02/25/2022)
        /// </summary>
        //TODO: JAH determine if this can be removed, or if we need to actually add a step where it's called.
        public static readonly Outbound835FileStatus Started = new("STARTED", 2);

        /// <summary>
        /// <see cref="Started"/> Included for completeness.
        /// </summary>
        public static readonly Outbound835FileStatus FinSegTxtStarted = new("FinSegTxtStarted", 12);
        /// <summary>
        /// <see cref="Started"/> Included for completeness.
        /// </summary>
        public static readonly Outbound835FileStatus FinSeg835Started = new("FinSeg835Started", 22);


        /// <summary>
        /// Status once the 835 file is completed and about to be sent to the FileTransferService
        /// CreateOutbound835File (not including link to avoid circular reference)
        /// </summary>
        public static readonly Outbound835FileStatus Built = new("BUILT", 3);
        /// <summary>
        /// Indicates that the FinSeq files have been built and are ready to be sent to Optum
        /// 
        /// </summary>
        public static readonly Outbound835FileStatus FinSegTxtBuilt = new("FinSegTxtBuilt", 13);
        /// <summary>
        /// Indicates that the FinSeq files have been built and are ready to be sent to Optum
        /// </summary>
        public static readonly Outbound835FileStatus FinSeg835Built = new("FinSeg835Built", 23);




        /// <summary>
        /// Message queued to the FileTransferService to send the files
        /// </summary>
        public static readonly Outbound835FileStatus Transferring = new("TRANSFERRING", 4);
        /// <summary>
        /// Will be set if any file fails to send (by Optum835PostTransferEventMessageConsumer)
        /// </summary>
        /// <remarks>Files will be left in ./optum_835/yyyy/MM/dd/</remarks>
        public static readonly Outbound835FileStatus FailedSend = new("FAILEDSEND", 5);
        /// <summary>
        /// Will be set if any file sends successfully (by Optum835PostTransferEventMessageConsumer)
        /// </summary>
        /// <remarks>These are copied from ./optum_835/yyyy/MM/dd/ to ./optum_835/success/yyyy/MM/dd/</remarks>
        public static readonly Outbound835FileStatus Completed = new("COMPLETED", 6);

        private Outbound835FileStatus(string name, int value) : base(name, value) { }
    }
}
