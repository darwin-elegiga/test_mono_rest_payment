namespace Vpay.Gateway.Common.Vpay.Models;

public class TransactionsTrn02DetailsDto
{
    public long VpayTransactionId { get; set; }
    public string? PaymentId { get; set; }
    public string? PaymentType { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string? ProviderTin { get; set; }
    public string? Trn02 { get; set; }
}
