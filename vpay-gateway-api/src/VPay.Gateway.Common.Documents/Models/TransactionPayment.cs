namespace VPay.Gateway.Common.Documents.Models;
public class TransactionPayment
{
    public long Id { get; set; }
    public long TransactionId { get; set; }
    public string ClientCode { get; set; } = string.Empty;
    public string BillingEntityCode { get; set; } = string.Empty;
    public string PaymentId { get; set; } = string.Empty;
}
