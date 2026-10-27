using System.Text;
using VPay.Gateway.Identity;

namespace VPay.Gateway.Api.Configuration;

/// <summary>
/// This sets up basic auth needed to get to the swagger document. It is based on https://stackoverflow.com/a/55637412
/// </summary>
public class SwaggerBasicAuthMiddleware
{
    private readonly RequestDelegate _next;

    public SwaggerBasicAuthMiddleware(RequestDelegate next)
    {
        this._next = next;
    }

    public async Task InvokeAsync(HttpContext context, IGetValidatedClientQuery getValidatedClientQuery)
    {
        //Make sure we are hitting the swagger path, and not doing it locally as it just gets annoying :-)
        if (context.Request.Path.StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase) && !this.IsLocalRequest(context))
        {
            string authHeader = context.Request.Headers["Authorization"];
            if (authHeader != null && authHeader.StartsWith("Basic "))
            {
                // Get the encoded username and password
                var encodedUsernamePassword = authHeader.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)[1]?.Trim();

                // Decode from Base64 to string
                var decodedUsernamePassword = Encoding.UTF8.GetString(Convert.FromBase64String(encodedUsernamePassword));

                // Split username and password
                var username = decodedUsernamePassword.Split(':', 2)[0];
                var password = decodedUsernamePassword.Split(':', 2)[1];

                var existingApiKey = await getValidatedClientQuery.GetUser(username, password).ConfigureAwait(false);

                // Check if login is correct
                if (existingApiKey != null)
                {
                    await _next.Invoke(context).ConfigureAwait(false);
                    return;
                }
            }

            // Return authentication type (causes browser to show login dialog)
            context.Response.Headers["WWW-Authenticate"] = "Basic";

            // Return unauthorized
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        }
        else
        {
            await _next.Invoke(context).ConfigureAwait(false);
        }
    }

    public bool IsLocalRequest(HttpContext context)
    {
        //Handle running using the Microsoft.AspNetCore.TestHost and the site being run entirely locally in memory without an actual TCP/IP connection
        if (context.Connection.RemoteIpAddress == null && context.Connection.LocalIpAddress == null)
        {
            return true;
        }
        if (context.Connection.RemoteIpAddress.Equals(context.Connection.LocalIpAddress))
        {
            return true;
        }
        if (IPAddress.IsLoopback(context.Connection.RemoteIpAddress))
        {
            return true;
        }
        return false;
    }
}
