using FluentValidation;
using VPay.Gateway.Application;

namespace VPay.Gateway.Api.Models;

public class PreferenceLookupDtoValidator : AbstractValidator<PreferenceLookupDto>
{
    public PreferenceLookupDtoValidator()
    {
        RuleFor(x => x.CorrelationID)
            .NotEmpty();

        RuleFor(x => x.PayerID)
            .NotEmpty()
            .MaximumLength(10);

        RuleFor(x => x.SubPayerID)
            .NotEmpty()
            .MaximumLength(10);

        RuleFor(x => x.TaxID)
            .NotEmpty()
            .MaximumLength(10);

        RuleFor(x => x.ZipCode)
            .NotEmpty()
            .Matches("^\\d{5}(-?\\d{4})?$");

        RuleFor(x => x.Amount)
            .NotNull()
            .WithMessage("Amount must be a valid number");

        RuleFor(x => x.ProviderAddress1)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.ProviderAddress1));
    }
}
