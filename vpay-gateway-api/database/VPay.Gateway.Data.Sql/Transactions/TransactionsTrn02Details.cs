namespace VPay.Gateway.Data.Sql.Transactions;

public class TransactionsTrn02Details
{
    public long VpayTransactionId { get; set; }

    public string? PaymentId { get; set; }

    public string? PaymentType { get; set; }

    public DateTime? PaymentDate { get; set; }

    public string? ProviderTin { get; set; }

    public string? Trn02 { get; set; }

    public string? ClientID { get; set; }

    public string? BillingEntity { get; set; }

}
