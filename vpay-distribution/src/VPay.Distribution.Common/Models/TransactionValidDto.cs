namespace VPay.Distribution.Common
{
    public class TransactionValidDto
    {
        public long TransactionId { get; set; }
        public bool IsValid { get; set; }
        public string Message { get; set; }
    }
}
