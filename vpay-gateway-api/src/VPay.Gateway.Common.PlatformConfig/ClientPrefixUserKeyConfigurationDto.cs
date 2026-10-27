using Newtonsoft.Json;

namespace VPay.Gateway.Common.PlatformConfig;

/// <summary>
/// Provides client configuration information to determine if a payment number (user key) for a given
/// client should be pre-pended with a prefix to make it unique across all other billing entities
/// for the same client.
/// </summary>
public class ClientPrefixUserKeyConfigurationDto
{
    /// <summary>
    /// The origin code of the for the client/billing entity configuration.
    /// </summary>
    [JsonProperty]
    public string OriginCode { get; set; } = default!;

    /// <summary>
    /// The client code of the for the client/billing entity configuration.
    /// </summary>
    [JsonProperty]
    public string ClientCode { get; set; } = default!;

    /// <summary>
    /// The billing entity code of the for the client/billing entity configuration.
    /// </summary>
    [JsonProperty]
    public string BillingEntityCode { get; set; } = default!;

    /// <summary>
    /// Flag to determine if the configuration is active for the given client/billing entity.
    /// </summary>
    [JsonProperty]
    public bool IsActive { get; set; } = default!;

    /// <summary>
    /// Flag to determine if the configuration is required for the given client/billing entity.
    /// </summary>
    [JsonProperty]
    public bool IsEnabled { get; set; } = default!;

    /// <summary>
    /// Flag to determine if a payment number (user key) for a given client or billing entity
    /// should be pre-pended with a prefix to make it unique across all other billing entities
    /// for the same client. This requires both the <see cref="IsActive"/> flag and the
    /// <see cref="IsEnabled"/> flag to be set.
    /// </summary>
    [JsonProperty]
    public bool IsPrefixRequired => IsActive && IsEnabled;
}
