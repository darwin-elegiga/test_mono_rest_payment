using System;

namespace VPay.Optum.Http.ReconReportsApi.Models
{
    public class BankAccountDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string AccountNumber { get; set; } = string.Empty;
        public string RouteNumber { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedDateTime { get; set; }
        public DateTime LastModifiedDateTime { get; set; }
    }
}
