namespace VPay.DocSys.Generation.Common
{
    public class AchFeeNoticeRequest
    {
        public string Name { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string AddressLine3 { get; set; }
        public long TransactionId { get; set; }
        public decimal LoadAmount { get; set; }
        public int PaymentsCount { get; set; }
        public decimal AchAmount { get; set; }
    }
}
