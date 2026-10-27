namespace VPay.Gateway.Common.Loaf;

public class CardDetailDto
{
    public long Id { get; set; }
    public string CreditCardNumber { get; set; }
    public string TransactionId { get; set; }
    public string CVC2 { get; set; }
    public bool ExactAmount { get; set; }
    public string PayeeName { get; set; }
    public string AddressLine1 { get; set; }
    public string AddressLine2 { get; set; }
    public string PostalCode { get; set; }
    public decimal? LoadedAmount { get; set; }
    public DateTime? StartDateTime { get; set; }
    public DateTime? EndDateTime { get; set; }
}
