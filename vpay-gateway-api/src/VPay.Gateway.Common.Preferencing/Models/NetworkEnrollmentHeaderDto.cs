using System.Diagnostics.CodeAnalysis;
using FluentValidation;

namespace VPay.Gateway.Common.Preferencing.Models;

[ExcludeFromCodeCoverage]
public class NetworkEnrollmentHeaderDto
{
    /// <summary>
    /// The name of the network partner.
    /// </summary>
    public string NetworkPartner { get; set; } = string.Empty;

    /// <summary>
    /// A unique identifier to match request with response.
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// The number of enrollments in the request.
    /// </summary>
    public int Count { get; set; }
}

public class NetworkEnrollmentHeaderDtoValidator : AbstractValidator<NetworkEnrollmentHeaderDto>
{
    public NetworkEnrollmentHeaderDtoValidator()
    {
        RuleFor(x => x.NetworkPartner)
            .NotEmpty()
            .WithMessage("{PropertyName} must be provided")
            .MaximumLength(12)
            .WithMessage("{PropertyName} must not exceed 12 characters");

        RuleFor(x => x.CorrelationId)
            .NotEmpty()
            .WithMessage("{PropertyName} must be provided")
            .MaximumLength(36)
            .WithMessage("{PropertyName} must not exceed 36 characters");

        RuleFor(x => x.Count)
            .GreaterThanOrEqualTo(0)
            .WithMessage("{PropertyName} must be greater than or equal to zero");
    }
}
