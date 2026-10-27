using VPay.Gateway.Common.Http;

namespace Vpay.Gateway.Common.Vpay;

/// <summary>
/// Configuration related specifically to the VPay 360 API.
/// </summary>
public class VPayApiHttpClientConfig : HttpClientConfig
{
    /// <summary>
    /// Gets or sets the access token for the VPay API.
    /// </summary>
    public string ApiToken { get; set; } = "";
}
