namespace VPay.Gateway.Management.Api.Models;

public class GatewayClientDto
{
    public long Id { get; set; }

    public string ClientId { get; set; }

    public string ClientCode { get; set; }

    public bool IsExpired { get; set; }

    public DateTime? ExpiredDateTime { get; set; }

    public string CreatedBy { get; set; }

    public DateTime CreatedDateTime { get; set; }

    public string LastModifiedBy { get; set; }

    public DateTime LastModifiedDateTime { get; set; }
}
