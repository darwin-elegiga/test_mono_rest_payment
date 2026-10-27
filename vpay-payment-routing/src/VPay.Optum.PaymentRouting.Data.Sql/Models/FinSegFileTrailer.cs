namespace VPay.Optum.PaymentRouting.Data.Sql.Models
{
    public class FinSegFileTrailer
    {
        public int DetailCount { get; set; }
        public int TotalRecords => DetailCount + 2; // Additional row count for header and trailer record (+2)
    }
}
