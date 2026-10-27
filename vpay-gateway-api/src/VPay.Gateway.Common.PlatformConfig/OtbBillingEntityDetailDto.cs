namespace VPay.Gateway.Common.PlatformConfig;

public record OtbBillingEntityDetailDto
{
    public string BillingEntityCode { get; init; } = string.Empty;
}
