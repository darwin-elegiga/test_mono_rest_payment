using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace VPay.Gateway.Identity.Events;

public class ClientCredentialsAuthenticationFailedContext : ResultContext<ClientCredentialsAuthenticationOptions>
{
    /// <summary>
    /// Creates a new instance of <see cref="ClientCredentialsAuthenticationFailedContext"/>.
    /// </summary>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <param name="context">The HttpContext the failure applies too.</param>
    /// <param name="scheme">The scheme used when the Basic authentication handler was registered.</param>
    /// <param name="options">The authentication options.</param>
    public ClientCredentialsAuthenticationFailedContext(
        Exception exception,
        HttpContext context,
        AuthenticationScheme scheme,
        ClientCredentialsAuthenticationOptions options)
        : base(context, scheme, options)
    {
        Exception = exception;
    }

    /// <summary>
    /// Gets the exception that caused the failure.
    /// </summary>
    public Exception Exception { get; }
}
