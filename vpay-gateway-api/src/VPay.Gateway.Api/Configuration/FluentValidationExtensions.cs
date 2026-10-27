using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

namespace VPay.Gateway.Api.Configuration;

public static class FluentValidationExtensions
{
    public static ValidationProblemDetails AsProblemDetails(this ValidationResult validationResult)
    {
        if (validationResult?.IsValid != false)
        {
            return null;
        }

        Dictionary<string, string[]> errors = GetErrors(validationResult.Errors);

        return new ValidationProblemDetails(errors);
    }

    public static ValidationProblemDetails AsProblemDetails(this ValidationException validationException)
    {
        if (validationException?.Errors.Any() != true)
        {
            return null;
        }

        Dictionary<string, string[]> errors = GetErrors(validationException.Errors);

        return new ValidationProblemDetails(errors);
    }

    private static Dictionary<string, string[]> GetErrors(IEnumerable<ValidationFailure> errorList)
    {
        var errorMessages = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var error in errorList)
        {
            if (errorMessages.TryGetValue(error.PropertyName, out List<string> messages))
            {
                messages.Add(error.ErrorMessage);
            }
            else
            {
                errorMessages[error.PropertyName] = [error.ErrorMessage];
            }
        }

        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        foreach (var errorMessage in errorMessages)
        {
            errors[errorMessage.Key] = errorMessage.Value.ToArray();
        }

        return errors;
    }
}
