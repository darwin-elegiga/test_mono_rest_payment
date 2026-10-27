using Microsoft.AspNetCore.Authentication;

namespace VPay.Gateway.Identity;

public static class ClientCredentialsAuthenticationExtensions
{
    public static AuthenticationBuilder AddClientCredentials(this AuthenticationBuilder builder) =>
        builder.AddClientCredentials(ClientCredentialsAuthenticationOptions.DefaultScheme);

    public static AuthenticationBuilder AddClientCredentials(this AuthenticationBuilder builder, string authenticationScheme) =>
        builder.AddClientCredentials(authenticationScheme, configureOptions: _ => { });

    public static AuthenticationBuilder AddClientCredentials(this AuthenticationBuilder builder, Action<ClientCredentialsAuthenticationOptions> configureOptions) =>
        builder.AddClientCredentials(ClientCredentialsAuthenticationOptions.DefaultScheme, configureOptions);

    public static AuthenticationBuilder AddClientCredentials(this AuthenticationBuilder builder, string authenticationScheme, Action<ClientCredentialsAuthenticationOptions> configureOptions)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder.AddScheme<ClientCredentialsAuthenticationOptions, ClientCredentialsAuthenticationHandler>(
            authenticationScheme,
            configureOptions);
    }
}
