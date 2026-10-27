using System;

namespace VPay.Optum.Http.ConfigApi.Models
{
    public class BillingEntity
    {
        public int Id { get; set; }

        public string ClientCode { get; set; } = "";

        public string BillingEntityCode { get; set; } = "";

        public string Description { get; set; } = "";

        public DateTime CreatedDateTime { get; set; }

        public string CreatedBy { get; set; } = "";

        public DateTime LastModifiedDateTime { get; set; }

        public string LastModifiedBy { get; set; } = "";
    }
}
