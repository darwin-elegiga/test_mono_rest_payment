namespace VPay.Optum.PaymentRouting.Common.Serialization
{
    public class PaymentSummary
    {
        /// <summary>
        /// The Optum Batch id that is passed on the messages
        /// </summary>
        public string BatchId { get; set; } = string.Empty;
        public string Payer835Id { get; set; } = string.Empty;
        public string TotalPaymentCount { get; set; } = string.Empty;
        public string TotalPaymentAmount { get; set; } = string.Empty;
    }
}
