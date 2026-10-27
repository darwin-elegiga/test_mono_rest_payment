namespace VPay.DocSys.Generation.Common.Models;

public class OAuthSettings
{
    public string IdServerUrl { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public string GrantType { get; set; } = string.Empty;
}
