using VPay.Gateway.Management.Application;

namespace VPay.Gateway.Management.Api.MvcCustomizations;

public class AttachUserToLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger _logger;

    public AttachUserToLoggingMiddleware(RequestDelegate next,
        ILoggerFactory loggerFactory)
    {
        _next = next;

        _logger = loggerFactory
            .CreateLogger<AttachUserToLoggingMiddleware>();
    }

    public async Task Invoke(HttpContext httpContext, IUserInfo userInfo)
    {
        if (string.IsNullOrWhiteSpace(userInfo.UserName) || !userInfo.IsAuthenticated)
        {
            await _next(httpContext).ConfigureAwait(false);
        }
        else
        {
            using (_logger.BeginScope(new Dictionary<string, object> { ["UserName"] = userInfo.UserName }))
            {
                await _next(httpContext).ConfigureAwait(false);
            }
        }
    }
}
