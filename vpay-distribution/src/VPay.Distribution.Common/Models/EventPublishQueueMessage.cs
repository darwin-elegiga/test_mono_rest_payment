namespace VPay.Distribution.Common
{
    public class EventPublishQueueMessage
    {
        public string ClientCode { get; set; }

        public string BillingEntityCode { get; set; }

        public string OriginCode { get; set; }

        public long TransactionId { get; set; }

        public string ClientPaymentId { get; set; }
    }
}
