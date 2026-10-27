namespace VPay.Gateway.Management.Api.Models;

public class GatewayClientCreateParam
{
    /// <summary>
    /// This is the client's id that is public
    /// </summary>
    /// <example></example>
    public string ClientId { get; set; }

    /// <summary>
    /// This is the client's secret and should be something the client secret
    /// </summary>
    /// <example></example>
    public string ClientSecret { get; set; }

    /// <summary>
    /// This is the Client/TPA Code that is associated with the client
    /// </summary>
    /// <example></example>
    public string ClientCode { get; set; }
}
