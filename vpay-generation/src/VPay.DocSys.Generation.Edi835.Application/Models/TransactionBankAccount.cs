namespace VPay.DocSys.Generation.Edi835.Application.Models
{
    public class TransactionBankAccount
    {
        public long TransactionId { get; set; }

        public string AccountNumber { get; set; }

        public string RouteNumber { get; set; }
    }
}
