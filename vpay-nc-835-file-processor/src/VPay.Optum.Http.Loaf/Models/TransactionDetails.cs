using System;

namespace VPay.Optum.Http.Loaf.Models
{
    public class TransactionDetails
    {
        public long? Id { get; set; }
        public string ReferenceId { get; set; } = "";
        public string FromAccountNumber { get; set; } = "";
        public string FromRoutingNumber { get; set; } = "";
        public string ToAccountNumber { get; set; } = "";
        public string ToRoutingNumber { get; set; } = "";
        public string CreatedBy { get; set; } = "";
        public DateTime CreatedDateTime { get; set; } = DateTime.Now;
        public string LastModifiedBy { get; set; } = "";
        public DateTime LastModifiedDateTime { get; set; }
    }
}
