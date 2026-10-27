namespace VPay.DocSys.Generation.Common
{
    public class OtbEntry
    {
        public OtbEntry(OtbDetailResponse obj)
        {
            BillingEntity = obj.BillingEntityCode;
            BillingEntityName = obj.BillingEntityFullName;

            Origin = obj.OriginCode;
            OriginName = obj.OriginName;

            TPA_Full_Name = obj.ClientFullName;
            TPA_Name = obj.ClientCode;

            TPA_Phone = obj.ClientPhone;
            VPay_Phone = obj.VPayPhone;
        }

        public string BillingEntity { get; set; }
        public string BillingEntityName { get; set; }

        public string Origin { get; set; }
        public string OriginName { get; set; }

        public string TPA_Full_Name { get; set; }
        public string TPA_Name { get; set; }

        public string TPA_Phone { get; set; }
        public string VPay_Phone { get; set; }
    }
}
