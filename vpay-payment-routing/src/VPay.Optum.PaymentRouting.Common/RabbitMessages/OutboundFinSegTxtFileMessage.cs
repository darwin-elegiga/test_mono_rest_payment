namespace VPay.Optum.PaymentRouting.Common.RabbitMessages
{
    /// <summary>
    /// Indicates that the FinSeg835 has been built and is ready to be sent to the client. This
    /// will create the summary FinSegTxt that acts as a control file.
    /// </summary>
    public record OutboundFinSegTxtFileMessage
    {
        /// <summary>
        /// This is the text file that will contain the summary for the client funded payments.
        /// Needs to be produced after the larger 835 file is built.
        /// </summary>
        public long OutboundFinSegTxtFileId { get; init; } = default!;
    }
}
