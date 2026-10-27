namespace VPay.Optum.PaymentRouting.Data.Sql.Models
{
    public class InboundHeader
    {
        public long InboundHeaderId { get; set; }
        public string PayerSubpayerId { get; set; }
        public string PayerAlias { get; set; }
        public string HeaderData { get; set; }
        public string Status { get; set; }
    }
}
