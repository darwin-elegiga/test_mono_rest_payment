namespace VPay.Gateway.Common.Preferencing.Models;

public class MonetizationResponse
{
    public required string CorrelationID { get; set; }
    public string? PaymentTypeCode { get; set; }
    public bool Monetized { get; set; }
}
