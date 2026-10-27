using VPay.Gateway.Common.Http;

namespace VPay.Gateway.Common.Loaf;

public class LoafApiHttpClientConfig : HttpClientConfig
{
    /// <summary>
    /// Gets or sets the access token for the Loaf API.
    /// </summary>
    public string ApiToken { get; set; } = "";
}
