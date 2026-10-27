using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace VPay.Gateway.Application;

#nullable enable

public struct PreferenceLookupDto
{
    /// <summary>
    /// Unique Identifier for request
    /// </summary>
    [NotNull]
    public string? CorrelationID { get; set; }
    /// <summary>
    /// Client-recognized payer identification
    /// </summary>
    [NotNull]
    public string? PayerID { get; set; }
    /// <summary>
    /// Client-recognized sub-payer identification
    /// </summary>
    [NotNull]
    public string? SubPayerID { get; set; }
    /// <summary>
    /// Payee tax id
    /// </summary>
    [NotNull]
    public string? TaxID { get; set; }
    /// <summary>
    /// Payee zip code
    /// </summary>
    [NotNull]
    public string? ZipCode { get; set; }
    /// <summary>
    /// Payment amount
    /// </summary>
    [DefaultValue(null)]
    public decimal? Amount { get; set; }
    /// <summary>
    /// Provider name
    /// </summary>
    [DefaultValue(null)]
    public string? ProviderName { get; set; }
    /// <summary>
    /// Provider address
    /// </summary>
    [DefaultValue(null)]
    public string? ProviderAddress1 { get; set; }
}
