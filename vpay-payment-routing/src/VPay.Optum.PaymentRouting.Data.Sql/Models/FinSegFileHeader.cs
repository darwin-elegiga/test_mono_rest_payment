using System;

namespace VPay.Optum.PaymentRouting.Data.Sql.Models
{
    public class FinSegFileHeader
    {
        public DateTime FileDate { get; set; } = DateTime.Now;

        public string FileName { get; set; } = string.Empty;
    }
}
