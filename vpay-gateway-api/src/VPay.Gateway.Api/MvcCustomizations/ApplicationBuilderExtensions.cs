namespace VPay.Gateway.Api.MvcCustomizations;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseUserLogging(
       this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AttachUserToLoggingMiddleware>();
    }
}
