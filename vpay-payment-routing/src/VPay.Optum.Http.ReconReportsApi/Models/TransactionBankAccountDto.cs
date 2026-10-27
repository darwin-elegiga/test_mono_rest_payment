namespace VPay.Optum.Http.ReconReportsApi.Models
{
    public class TransactionBankAccountDto
    {
        public long TransactionId { get; set; }
        public string AccountNumber { get; set; } = string.Empty;
        public string RouteNumber { get; set; } = string.Empty;
    }
}
