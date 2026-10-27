using FluentValidation;

namespace VPay.Gateway.Common.Preferencing.Models;

public class NetworkEnrollmentRequestDto
{
    public NetworkEnrollmentHeaderDto Header { get; set; } = new NetworkEnrollmentHeaderDto();

    public IEnumerable<NetworkEnrollmentListDto> PartnerProviders { get; set; } = new List<NetworkEnrollmentListDto>();
}

public class NetworkEnrollmentRequestDtoValidator : AbstractValidator<NetworkEnrollmentRequestDto>
{
    public NetworkEnrollmentRequestDtoValidator()
    {
        RuleFor(x => x.Header)
            .NotEmpty()
            .NotNull()
            .WithMessage("{PropertyName} must be provided");

        RuleFor(x => x.PartnerProviders)
            .NotEmpty()
            .NotNull()
            .WithMessage("{PropertyName} must be provided");

        #region Child Validation

        RuleFor(x => x.Header)
            .SetValidator(new NetworkEnrollmentHeaderDtoValidator());

        RuleForEach(x => x.PartnerProviders)
            .SetValidator(new NetworkEnrollmentListDtoValidator());

        #endregion
    }
}
