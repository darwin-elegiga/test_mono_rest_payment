using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace VPay.Gateway.Identity.Events;

public class ClientCredentialsValidateCredentialsContext : ResultContext<ClientCredentialsAuthenticationOptions>
{
    /// <summary>
    /// Creates a new instance of <see cref="ClientCredentialsValidateCredentialsContext"/>.
    /// </summary>
    /// <param name="clientId">The username to be validated.</param>
    /// <param name="clientSecret">The password to be validated.</param>
    /// <param name="context">The HttpContext the validate context applies too.</param>
    /// <param name="scheme">The scheme used when the Basic authentication handler was registered.</param>
    /// <param name="options">The authentication options.</param>
    public ClientCredentialsValidateCredentialsContext(
        string clientId,
        string clientSecret,
        HttpContext context,
        AuthenticationScheme scheme,
        ClientCredentialsAuthenticationOptions options)
        : base(context, scheme, options)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new ArgumentException($"'{nameof(clientId)}' cannot be null or whitespace", nameof(clientId));
        }

        if (string.IsNullOrWhiteSpace(clientSecret))
        {
            throw new ArgumentException($"'{nameof(clientSecret)}' cannot be null or whitespace", nameof(clientSecret));
        }

        ClientId = clientId;
        ClientSecret = clientSecret;
    }

    /// <summary>
    /// The user name to validate.
    /// </summary>
    public string ClientId { get; }

    /// <summary>
    /// The password to validate.
    /// </summary>
    public string ClientSecret { get; }
}
