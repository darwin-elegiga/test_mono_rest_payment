using FluentValidation;

namespace VPay.Gateway.Management.Api.Models;

public class GuidewireEnrollmentStatusChangeDtoValidator : AbstractValidator<GuidewireEnrollmentStatusChangeDto>
{
    public GuidewireEnrollmentStatusChangeDtoValidator()
    {
        RuleFor(x => x.ClientCode)
            .NotEmpty()
            .MaximumLength(10);

        RuleFor(x => x.PayeeId)
            .NotEmpty();

        RuleFor(x => x.Status)
            .NotEmpty();

        RuleFor(x => x.Note)
            .SetValidator(new GuidewireNoteDtoValidator());
    }
}
