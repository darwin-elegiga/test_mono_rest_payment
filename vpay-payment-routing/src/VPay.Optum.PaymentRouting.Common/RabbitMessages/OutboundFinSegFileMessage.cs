namespace VPay.Optum.PaymentRouting.Common.RabbitMessages
{
    /// <summary>
    /// Indicates that the 835 and FINSEG TXT for client funded (Optum, in this case)
    /// payments, CLCFT, are ready to be built from available data.
    /// </summary>
    public record OutboundFinSegFileMessage
    {
        public long OutboundFinSeg835FileId { get; init; } = default!;

        public long OutboundFinSegTxtFileId { get; init; } = default!;
    };

}
