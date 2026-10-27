using Microsoft.Extensions.Options;
using VPay.Gateway.Application;

namespace VPay.Gateway.Api.MvcCustomizations;
using Hellang.Middleware.ProblemDetails;
public class ProblemDetailsOptionsCustomSetup : IConfigureOptions<ProblemDetailsOptions>
{
    public ProblemDetailsOptionsCustomSetup(IWebHostEnvironment environment)
    {
        Environment = environment;
    }

    private IWebHostEnvironment Environment { get; }

    public void Configure(ProblemDetailsOptions options)
    {
        options.ValidationProblemStatusCode = StatusCodes.Status400BadRequest;

        options.IncludeExceptionDetails = (_, _) => Environment.IsDevelopment() || Environment.IsEnvironment("LocalDevelopment");

        // This will map NotImplementedException to the 501 Not Implemented status code.
        options.MapToStatusCode<NotImplementedException>(StatusCodes.Status501NotImplemented);

        options.MapToStatusCode<NoPrsConfigurationValidationException>(StatusCodes.Status501NotImplemented);
    }
}
