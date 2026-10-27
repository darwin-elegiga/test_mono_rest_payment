namespace VPay.Gateway.Api.Models;

public class MonetizationResultDto
{
    public string CorrelationId { get; set; } = string.Empty;

    public bool ValidatedPayee { get; set; }
}
