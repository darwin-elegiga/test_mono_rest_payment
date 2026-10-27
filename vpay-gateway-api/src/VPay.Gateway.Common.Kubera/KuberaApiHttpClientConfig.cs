using VPay.Gateway.Common.Http;

namespace VPay.Gateway.Common.Kubera;

/// <summary>
/// Configuration related specifically to the Kubera API.
/// </summary>
public class KuberaApiHttpClientConfig : HttpClientConfig
{
    /// <summary>
    /// Gets or sets the per-call timespan to wait before the call times out.
    /// </summary>
    public TimeSpan PerCallTimeout { get; set; } = TimeSpan.FromSeconds(30);
}
