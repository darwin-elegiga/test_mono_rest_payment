namespace VPay.Gateway.Common.PlatformConfig;

public class BillingEntityTypeDto
{
    public int Id { get; set; }

    public string Name { get; set; } = default!;

    public string CreatedBy { get; set; } = default!;

    public DateTime CreatedDateTime { get; set; }

    public string LastModifiedBy { get; set; } = default!;

    public DateTime LastModifiedDateTime { get; set; }

    public string Description { get; set; } = default!;
}
