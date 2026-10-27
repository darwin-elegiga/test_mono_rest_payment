namespace VPay.Optum.Http.Loaf.Models
{
    public class TransactionBankInfoUpsert
    {
        public string ReferenceId { get; set; } = "";
        public string FromAccountNumber { get; set; } = "";
        public string FromRoutingNumber { get; set; } = "";
        public string ToAccountNumber { get; set; } = "";
        public string ToRoutingNumber { get; set; } = "";
    }
}
