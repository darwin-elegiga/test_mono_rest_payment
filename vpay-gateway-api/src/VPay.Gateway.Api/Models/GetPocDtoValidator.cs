using FluentValidation;

namespace VPay.Gateway.Api.Models;

public class GetPocDtoValidator : AbstractValidator<GetPocDto>
{
    public GetPocDtoValidator()
    {
        RuleFor(x => x.BillingEntityId)
            .NotEmpty()
            .MaximumLength(10);

        RuleFor(x => x.TaxId)
            .NotEmpty()
            .MaximumLength(10);

        RuleFor(x => x.ZipCode)
            .Matches(@"^\d{5}(-?\d{4})?$")
            .When(x => !string.IsNullOrEmpty(x.ZipCode));

        RuleFor(x => x.Amount)
            .NotNull();
    }
}
