namespace VPay.DocSys.Parsing.Common
{
    public class OutboundRedCardDataDriverMessage
    {
        public long PaymentStatusFileId { get; set; }
        public string DataDriverFileName { get; set; }
        public OutboundRedCardDataDriverFile DriverFile { get; set; }
    }
}
