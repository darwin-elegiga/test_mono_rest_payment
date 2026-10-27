namespace VPay.Gateway.Common.Documents.Models;
public class TransactionDetails
{
    public long VpayTransactionId { get; set; }

    public string? PaymentId  { get; set; }

    public string? PaymentType { get; set; }

    public DateTime? PaymentDate { get; set; }

    public string? ProviderTin { get; set; }
}
