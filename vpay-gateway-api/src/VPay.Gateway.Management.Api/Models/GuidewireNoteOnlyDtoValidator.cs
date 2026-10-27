using FluentValidation;

namespace VPay.Gateway.Management.Api.Models;

public class GuidewireNoteOnlyDtoValidator : AbstractValidator<GuidewireNoteOnlyDto>
{
    public GuidewireNoteOnlyDtoValidator()
    {
        RuleFor(x => x.ClientCode)
            .NotEmpty()
            .MaximumLength(10);

        Include(new GuidewireNoteDtoValidator());
    }
}
