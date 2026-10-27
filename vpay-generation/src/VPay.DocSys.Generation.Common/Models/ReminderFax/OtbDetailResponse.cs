namespace VPay.DocSys.Generation.Common
{
    public class OtbDetailResponse
    {
        public string OriginCode { get; set; }
        public string OriginName { get; set; }

        public string ClientCode { get; set; }
        public string ClientFullName { get; set; }

        public string BillingEntityCode { get; set; }
        public string BillingEntityFullName { get; set; }

        public string VPayPhone { get; set; }
        public string ClientPhone { get; set; }
    }
}
