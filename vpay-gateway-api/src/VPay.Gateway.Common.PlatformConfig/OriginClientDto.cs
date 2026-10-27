namespace VPay.Gateway.Common.PlatformConfig;

public class OriginClientDto
{
    public int OriginClientId { get; set; }
    public OriginInfoDto Origin { get; set; } = new OriginInfoDto();
    public string ClientCode { get; set; } = string.Empty;
    public string BillingEntityCode { get; set; } = string.Empty;
    public string PaymentTypeCode { get; set; } = string.Empty;
    public string SwitchTypeCode { get; set; } = string.Empty;
    public DateTime BeginDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime CreateDatetime { get; set; }
    public string CreatedByProgram { get; set; } = string.Empty;
    public string CreatedByUser { get; set; } = string.Empty;
    public DateTime LastModifiedDateTime { get; set; }
    public string LastModifiedByProgram { get; set; } = string.Empty;
    public string LastModifiedByUser { get; set; } = string.Empty;
}
