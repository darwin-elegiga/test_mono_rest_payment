namespace VPay.Distribution.Common
{
    public class TransactionExistsDto
    {
        public long TransactionId { get; set; }
        public bool Exists { get; set; }
        public string Message { get; set; }
    }
}
