using System;

namespace VPay.Optum.PaymentRouting.Data.Sql.Models
{
    public class FinSegFileDetail
    {
        public string PayerId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime ProcessDate { get; set; }
    }
}
