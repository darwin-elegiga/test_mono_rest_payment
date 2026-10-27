using FluentValidation;
using VPay.Gateway.Application;

namespace VPay.Gateway.Api.Models;

public class PreferenceLookupDtoListValidator : AbstractValidator<PreferenceLookupDtoList>
{
    public PreferenceLookupDtoListValidator(IValidator<PreferenceLookupDto> dtoValidator)
    {
        RuleFor(items => items.Count)
            .LessThanOrEqualTo(1000);

        RuleForEach(items => items)
            .Must((all, item) => all.Count(i => i.CorrelationID == item.CorrelationID) < 2)
            .WithMessage((_, item) => $"'{nameof(PreferenceLookupDto.CorrelationID)}' must be unique (duplicate value '{item.CorrelationID}')")
            .SetValidator(dtoValidator);
    }
}
