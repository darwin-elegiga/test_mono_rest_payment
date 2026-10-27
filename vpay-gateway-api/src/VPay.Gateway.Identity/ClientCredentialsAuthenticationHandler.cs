using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VPay.Gateway.Identity.Events;

namespace VPay.Gateway.Identity;

public class ClientCredentialsAuthenticationHandler : AuthenticationHandler<ClientCredentialsAuthenticationOptions>
{
    private const string ClientIdHeaderName = "X-Client-Id";
    private const string ClientSecretHeaderName = "X-Client-Secret";

    public ClientCredentialsAuthenticationHandler(
        IOptionsMonitor<ClientCredentialsAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock) : base(options, logger, encoder, clock)
    {
    }

    /// <summary>
    /// This handler calls methods on the events to give the application control at different points where processing occurs.
    /// If this is not provided, the default instance is supplied, which does nothing when methods are called.
    /// </summary>
    protected new ClientCredentialsAuthenticationEvents Events
    {
        get => (ClientCredentialsAuthenticationEvents)base.Events;
        set => base.Events = value;
    }

    protected override Task<object> CreateEventsAsync() =>
        Task.FromResult<object>(new ClientCredentialsAuthenticationEvents());

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        try
        {
            var getCredentials = CredentialsFromHeader();

            if (getCredentials == null)
            {
                //Logger.LogWarning("No credentials or invalid credentials in header");
                return AuthenticateResult.NoResult();
            }

            await Events.ValidateCredentials(getCredentials).ConfigureAwait(false);

            if (getCredentials.Result?.Succeeded == true)
            {
                var ticket = new AuthenticationTicket(getCredentials.Principal, Scheme.Name);
                return AuthenticateResult.Success(ticket);
            }

            if (getCredentials.Result?.Failure != null)
            {
                Logger.LogError(getCredentials.Result.Failure, "Authentication failed: {ErrorMessage}", getCredentials.Result.Failure.Message);
                return AuthenticateResult.Fail(getCredentials.Result.Failure);
            }

            Logger.LogWarning("No result from credentials in header");
            return AuthenticateResult.NoResult();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Authentication failed");
            throw;
        }
    }

    private ClientCredentialsValidateCredentialsContext? CredentialsFromHeader()
    {
        if (!Request.Headers.TryGetValue(ClientIdHeaderName, out var clientIdHeaderValues))
        {
            return null;
        }

        var providedClientId = clientIdHeaderValues.FirstOrDefault();

        if (clientIdHeaderValues.Count == 0 || string.IsNullOrWhiteSpace(providedClientId))
        {
            return null;
        }

        if (!Request.Headers.TryGetValue(ClientSecretHeaderName, out var clientKeyHeaderValues))
        {
            return null;
        }

        var providedClientSecret = clientKeyHeaderValues.FirstOrDefault();

        if (clientKeyHeaderValues.Count == 0 || string.IsNullOrWhiteSpace(providedClientSecret))
        {
            return null;
        }

        return new ClientCredentialsValidateCredentialsContext(providedClientId, providedClientSecret, Context, Scheme, Options);
    }
}
