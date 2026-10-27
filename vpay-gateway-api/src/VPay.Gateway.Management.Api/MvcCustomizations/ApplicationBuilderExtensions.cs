namespace VPay.Gateway.Management.Api.MvcCustomizations;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseUserLogging(
       this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AttachUserToLoggingMiddleware>();
    }
}
