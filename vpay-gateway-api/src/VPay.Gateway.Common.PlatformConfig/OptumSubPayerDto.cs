namespace VPay.Gateway.Common.PlatformConfig;

public class OptumSubPayerDto
{
    public string ClientCode { get; set; } = default!;

    public string BillingEntityCode { get; set; } = default!;

    public string OptumPayorId { get; set; } = default!;

    public string OptumSubPayorId { get; set; } = default!;

    public DateTime CreatedDateTime { get; set; }

    public string CreatedBy { get; set; } = default!;

    public DateTime LastModifiedDateTime { get; set; }

    public string LastModifiedBy { get; set; } = default!;
}
