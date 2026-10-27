using VPay.Gateway.Common.Identity;
using VPay.Gateway.Identity;

namespace VPay.Gateway.Api.Authentication;

/// <summary>
/// Extensions related to authentication.
/// </summary>
public static class AuthenticationExtensions
{
    /// <summary>
    /// Add authentication related to the Gateway.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> that services are added to.</returns>
    public static IServiceCollection AddGatewayAuthentication(this IServiceCollection services) =>
        services
            .AddTransient<GatewayAuthenticationEvents>()
            .AddTransient<IPasswordHasher, PasswordHasher>()
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = ClientCredentialsAuthenticationOptions.DefaultScheme;
                options.DefaultChallengeScheme = ClientCredentialsAuthenticationOptions.DefaultScheme;
            })
            .AddClientCredentials(options =>
            {
                options.EventsType = typeof(GatewayAuthenticationEvents);
            })
            .Services;
}
