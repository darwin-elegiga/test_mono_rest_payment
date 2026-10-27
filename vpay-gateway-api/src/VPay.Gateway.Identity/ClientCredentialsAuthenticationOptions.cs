using Microsoft.AspNetCore.Authentication;
using VPay.Gateway.Identity.Events;

namespace VPay.Gateway.Identity;

/// <summary>
/// Options used by the basic authentication.
/// </summary>
public class ClientCredentialsAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string DefaultScheme = "Client Credentials";
    public string Scheme => DefaultScheme;
    public string AuthenticationType => DefaultScheme;

    /// <summary>
    /// Gets or sets the Realm sent in the WWW-Authenticate header.
    /// </summary>
    /// <remarks>
    /// The realm value (case-sensitive), in combination with the canonical root URL
    /// of the server being accessed, defines the protection space.
    /// These realms allow the protected resources on a server to be partitioned into a
    /// set of protection spaces, each with its own authentication scheme and/or
    /// authorization database.
    /// </remarks>
    public string? Realm { get; set; }

    /// <summary>
    /// The object that will process events raised by the basic authentication handler.
    /// </summary>
    public new ClientCredentialsAuthenticationEvents Events
    {
        get => (ClientCredentialsAuthenticationEvents)base.Events;
        set => base.Events = value;
    }
}
