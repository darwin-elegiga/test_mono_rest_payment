using FluentValidation;

namespace VPay.Gateway.Management.Api.Models;

public class GuidewireNoteDtoValidator : AbstractValidator<GuidewireNoteDto>
{
    public GuidewireNoteDtoValidator()
    {
        RuleFor(x => x.Subject)
            .NotEmpty();

        RuleFor(x => x.Body)
            .NotEmpty();

        RuleFor(x => x.Topic)
            .NotEmpty();
    }
}
