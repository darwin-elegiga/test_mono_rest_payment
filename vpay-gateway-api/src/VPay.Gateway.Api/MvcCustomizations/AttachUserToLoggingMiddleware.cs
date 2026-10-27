using VPay.Gateway.Application;

namespace VPay.Gateway.Api.MvcCustomizations;

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
        if (!userInfo.IsAuthenticated)
        {
            await _next(httpContext).ConfigureAwait(false);
        }
        else
        {
            using (_logger.BeginScope(new Dictionary<string, object> { ["ClientId"] = userInfo.ClientId, ["DisplayName"] = userInfo.DisplayName, ["UserId"] = userInfo.UserId }))
            {
                await _next(httpContext).ConfigureAwait(false);
            }
        }
    }
}
