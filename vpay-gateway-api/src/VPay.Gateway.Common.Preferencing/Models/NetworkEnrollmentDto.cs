using FluentValidation;

namespace VPay.Gateway.Common.Preferencing.Models;

public class NetworkEnrollmentDto
{
    public NetworkEnrollmentRequestDto PartnerProviderEnrollmentRequest { get; } = new NetworkEnrollmentRequestDto();
}

public class NetworkEnrollmentDtoValidator : AbstractValidator<NetworkEnrollmentDto>
{
    public NetworkEnrollmentDtoValidator()
    {
        RuleFor(x => x.PartnerProviderEnrollmentRequest)
            .NotEmpty()
            .WithMessage("{PropertyName} must be provided");
    }
}
