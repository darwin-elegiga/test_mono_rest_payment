namespace VPay.DocSys.Generation.Edi835.Application.Models
{
    public class PayfileDetail
    {
        public string TransactionId { get; set; }

        public string OriginCode { get; set; }

        public string ClientCode { get; set; }

        public string BillingEntityCode { get; set; }

        public string ClaimId { get; set; }

        public string CheckNumber { get; set; }

        public string CheckDate { get; set; }

        public string UserKey { get; set; }

        public string PaymentTypeCode { get; set; }

        public string Amount { get; set; }

        public string Currency { get; set; }

        public string SesPayDate { get; set; }

        public string PortUser { get; set; } = "PORTUSER";

        public string TaxId { get; set; }

        public string TaxIdSuffix { get; set; }

        public string NPPID { get; set; }

        public string Name1 { get; set; }

        public string Name2 { get; set; }

        public string Address1 { get; set; }

        public string Address2 { get; set; }

        public string Address3 { get; set; }

        public string Address4 { get; set; }

        public string City { get; set; }

        public string State { get; set; }

        public string PostalCode { get; set; }

        public string Country { get; set; }

        public string EftFromRoute { get; set; }

        public string EftFromAccount { get; set; }

        public string EftToRoute { get; set; }

        public string EftToAccount { get; set; }
    }
}
