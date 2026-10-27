using VPay.Gateway.Common.Http;

namespace VPay.Gateway.Common.PlatformConfig;

public class PlatformConfigApiHttpClientConfig : HttpClientConfig
{
    /// <summary>
    /// Gets or sets the access token for the Platform Configuration API.
    /// </summary>
    public string ApiToken { get; set; } = "";
}
