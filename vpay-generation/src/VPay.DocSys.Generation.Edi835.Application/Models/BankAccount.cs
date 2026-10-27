using System;

namespace VPay.DocSys.Generation.Edi835.Application.Models
{
    public class BankAccount
    {
        public int Id { get; set; }

        public string Code { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public string AccountNumber { get; set; }

        public string RouteNumber { get; set; }

        public string CreatedBy { get; set; }

        public DateTime CreatedDateTime { get; set; }

        public string LastModifiedBy { get; set; }

        public DateTime LastModifiedDateTime { get; set; }
    }
}
