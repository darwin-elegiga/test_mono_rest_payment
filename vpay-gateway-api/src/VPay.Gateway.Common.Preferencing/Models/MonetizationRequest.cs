namespace VPay.Gateway.Common.Preferencing.Models;

public class MonetizationRequest
{
    public string CorrelationID { get; set; } = string.Empty;
    public string OriginCode { get; set; } = string.Empty;
    public string ClientCode { get; set; } = string.Empty;
    public string? BillingEntityCode { get; set; }
    public string? TaxId { get; set; }
    public string? ProviderName { get; set; }
    public string? ProviderAddress1 { get; set; }
    public string? ProviderAddress2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    /// <summary>
    /// This is used to determine defaults if a preference is not found
    /// </summary>
    /// <example>MEDICAL</example>
    public string ProviderType { get; set; } = string.Empty;
    public string? ZipCode { get; set; }
    public string? Plus4 { get; set; }
    public decimal? Amount { get; set; }
    public DateTime Date { get; set; }
}
