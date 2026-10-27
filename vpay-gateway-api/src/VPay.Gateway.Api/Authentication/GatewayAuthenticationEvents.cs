using System.Security.Claims;
using VPay.Gateway.Identity;
using VPay.Gateway.Identity.Events;

namespace VPay.Gateway.Api.Authentication;

/// <summary>
/// An implementation of the authentication events to hook into processing of authentication.
/// </summary>
public class GatewayAuthenticationEvents : ClientCredentialsAuthenticationEvents
{
    private readonly ILogger<GatewayAuthenticationEvents> _logger;
    private readonly IGetValidatedClientQuery _getValidatedClientQuery;

    public GatewayAuthenticationEvents(
        ILogger<GatewayAuthenticationEvents> logger,
        IGetValidatedClientQuery getValidatedClientQuery)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _getValidatedClientQuery = getValidatedClientQuery;
    }

    public override async Task ValidateCredentials(ClientCredentialsValidateCredentialsContext context)
    {
        var user = await _getValidatedClientQuery.GetUser(context.ClientId, context.ClientSecret).ConfigureAwait(false);

        if (user != null)
        {
            var claims = new List<Claim>
                    {
                        new Claim(ClaimNames.ClientId, context.ClientId),
                        new Claim(ClaimNames.DisplayName, user.DisplayName),
                        new Claim(ClaimNames.EntityId, user.UserId.ToString())
                    };

            claims.AddRange(user.Permissions.Select(role => new Claim(ClaimTypes.Role, role.Name)));

            if (user.AccessGroup != null)
            {
                claims.AddRange(user.AccessGroup.ClientFilter
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(client => new Claim(ClaimNames.ClientCode, client)));

                claims.AddRange(user.AccessGroup.BillingEntityFilter
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(be => new Claim(ClaimNames.BillingEntityCode, be)));

                claims.AddRange(user.AccessGroup.BillingEntityExclusions
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(exc => new Claim(ClaimNames.BillingEntityExclusion, exc)));
            }

            context.Principal = new ClaimsPrincipal(new List<ClaimsIdentity> { new ClaimsIdentity(claims, context.Scheme.Name) });
            context.Success();
        }
        else
        {
            _logger.LogWarning("Unable to validate credentials");
            context.Fail("Invalid ClientId / ClientSecret provided.");
        }
    }
}
