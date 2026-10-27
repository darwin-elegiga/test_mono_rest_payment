namespace VPay.Gateway.Common.Http;

/// <summary>
/// Base class for configuration of HttpClients.
/// </summary>
public class HttpClientConfig
{
    /// <summary>
    /// Gets or sets the base address of the URI used when sending requests.
    /// </summary>
    public Uri? BaseAddress { get; set; }

    /// <summary>
    /// Gets or sets the timespan to wait before the request times out.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}
