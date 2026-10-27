namespace VPay.Gateway.Management.Api.Configuration;

public class RedisConfigurationOptions
{
    /// <summary>
    /// The password to use to authenticate with the server.
    /// </summary>
    public string Password { get; set; }

    /// <summary>
    /// Indicates whether the connection should be encrypted
    /// </summary>
    public bool Ssl { get; set; }

    /// <summary>
    /// Indicates whether admin operations should be allowed
    /// </summary>
    public bool AllowAdmin { get; set; }

    /// <summary>
    /// The number of times to repeat the initial connect cycle if no servers respond promptly
    /// </summary>
    public int ConnectRetry { get; set; }

    /// <summary>
    /// Specifies the time in milliseconds that should be allowed for connection (defaults to 5 seconds unless SyncTimeout is higher)
    /// </summary>
    public int ConnectTimeout { get; set; }

    /// <summary>
    /// The host:port strings to add an endpoint for.
    /// </summary>
    public string[] Hosts { get; set; }
}
