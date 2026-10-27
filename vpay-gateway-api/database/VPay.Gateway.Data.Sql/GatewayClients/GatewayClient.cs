namespace VPay.Gateway.Data.Sql.GatewayClients;

public class GatewayClient
{
    public long Id { get; set; }

    public string ClientId { get; set; } = "";

    public string ClientSecret { get; set; } = "";

    public string OriginCode { get; set; } = "";

    public string ClientCode { get; set; } = "";

    public string CreatedBy { get; set; } = "";

    public DateTime? ExpiredDateTime { get; set; }

    public string LastModifiedBy { get; set; } = "";

    public DateTime CreatedDateTime { get; set; }

    public DateTime LastModifiedDateTime { get; set; }
}
