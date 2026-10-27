using System;

namespace VPay.Optum.Http.PreferencingApi.Models
{
    public class OptumAchEnrollmentDto
    {
        public long Id { get; set; }

        public string TaxId { get; set; }

        public string NationalProviderId { get; set; }

        public char Status { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public string CreatedBy { get; set; }

        public DateTime CreatedDateTime { get; set; }

        public string LastModifiedBy { get; set; }

        public DateTime LastModifiedDateTime { get; set; }

        public string ClientCode { get; set; }

        public string ProviderName { get; set; }

        public string PaymentMethodCode { get; set; }
    }
}
