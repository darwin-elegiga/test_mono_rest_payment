using VPay.Gateway.Common.Http;

namespace VPay.Gateway.Common.Recon;

/// <summary>
/// Configuration related specifically to the Recon API.
/// </summary>
public class ReconApiHttpClientConfig : HttpClientConfig
{
    /// <summary>
    /// Gets or sets the access token for the Recon API.
    /// </summary>
    public string ApiToken { get; set; } = "";
}
