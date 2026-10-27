namespace VPay.Gateway.Common.Preferencing.Models;

public class MonetizationResult
{
    public string CorrelationId { get; set; } = string.Empty;

    public bool ValidatedPayee { get; set; }
}
