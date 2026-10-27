using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;


namespace VPay.Gateway.Api.MvcCustomizations;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class FeatureFlagAttribute : Attribute, IAsyncActionFilter
{
    private readonly string _featureName;
    public FeatureFlagAttribute(string featureName)
    {
        _featureName = featureName;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Resolve IConfiguration from the request's service provider
        if (string.Equals(_featureName, "IsZetaEnabled", StringComparison.OrdinalIgnoreCase))
        {
            var config = context.HttpContext.RequestServices.GetService(typeof(IConfiguration)) as IConfiguration;
            var isFeatureEnabled = config?.GetSection("FeatureFlag:IsZetaEnabled").Value;

            var loggerFactory = context.HttpContext.RequestServices.GetService(typeof(ILoggerFactory)) as ILoggerFactory;
            var _logger = loggerFactory?.CreateLogger<FeatureFlagAttribute>();
            _logger.LogInformation("feature flag picked from vault: {featureflag}", isFeatureEnabled);

            if (string.Equals(isFeatureEnabled, "false", StringComparison.OrdinalIgnoreCase))
            {
                context.Result = new NotFoundResult();
                return;
            }
        }
        await next();
    }
}
