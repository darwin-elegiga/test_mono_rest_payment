using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using VPay.Gateway.Application;

namespace VPay.Gateway.Api.MvcCustomizations;

public class ValidationExceptionFilterAttribute : ExceptionFilterAttribute
{
    public override void OnException(ExceptionContext context)
    {
        var factory = context.HttpContext?.RequestServices?.GetRequiredService<ProblemDetailsFactory>();

        if (context.Exception is FluentValidation.ValidationException validation)
        {
            var result = new FluentValidation.Results.ValidationResult(validation.Errors);
            result.AddToModelState(context.ModelState);
            context.Result = new BadRequestObjectResult(
                factory.CreateValidationProblemDetails(
                    context.HttpContext, context.ModelState, StatusCodes.Status400BadRequest));
        }

        if (context.Exception is NoPrsConfigurationValidationException notImplemented)
        {
            var result = new FluentValidation.Results.ValidationResult(notImplemented.Errors);
            result.AddToModelState(context.ModelState);
            context.Result = new ObjectResult(
                factory.CreateValidationProblemDetails(
                    context.HttpContext, context.ModelState, StatusCodes.Status501NotImplemented));
        }
    }
}
