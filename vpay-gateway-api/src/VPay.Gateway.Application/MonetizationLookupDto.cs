namespace VPay.Gateway.Application;

public class MonetizationLookupDto
{
    public string CorrelationID { get; set; } = string.Empty;
    public string ClientCode { get; set; } = string.Empty;
    public string BillingEntityCode { get; set; } = string.Empty;
    public string? TaxId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string ProviderAddress1 { get; set; } = string.Empty;
    public string? ProviderAddress2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string? Plus4 { get; set; }
    public decimal Amount { get; set; }
}
