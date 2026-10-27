namespace VPay.Gateway.Common.PlatformConfig;

public class BillingEntityDto
{
    public string ClientCode { get; set; } = default!;

    public string BillingEntityCode { get; set; } = default!;

    public string Description { get; set; } = default!;

    public string RecordStatusCode { get; set; } = default!;

    public int BillingEntityTypeId { get; set; }

    public string LedgerNaturalAccount { get; set; } = default!;

    public string LedgerSubAccount { get; set; } = default!;

    public BillingEntityTypeDto Type { get; set; } = default!;

    public string CreatedBy { get; set; } = default!;

    public DateTime CreatedDateTime { get; set; }

    public string LastModifiedBy { get; set; } = default!;

    public DateTime LastModifiedDateTime { get; set; }
}
