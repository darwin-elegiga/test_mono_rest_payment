using FluentValidation;
using VPay.Gateway.Application;

namespace VPay.Gateway.Api.Models;

public class MonetizationLookupDtoValidator : AbstractValidator<MonetizationLookupDto>
{
    public MonetizationLookupDtoValidator()
    {
        RuleFor(x => x.CorrelationID)
           .NotEmpty()
           .NotNull()
           .WithMessage("correlationID must be a valid value");

        RuleFor(x => x.ClientCode)
             .Cascade(CascadeMode.Stop)
             .NotNull()
             .NotEmpty()
             .WithMessage("ClientCode must not be empty or null.")
             .Must(value => !string.IsNullOrWhiteSpace(value))
             .MaximumLength(10)
             .WithMessage("ClientCode must be a valid string");

        RuleFor(x => x.BillingEntityCode)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .NotEmpty()
            .WithMessage("BillingEntityCode must not be empty or null.")
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .MaximumLength(10)
            .WithMessage("BillingEntityCode must be a valid string");

        RuleFor(x => x.TaxId)
            .MaximumLength(10);

        RuleFor(x => x.ProviderName)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .NotEmpty()
            .WithMessage("ProviderName must not be empty or null.")
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("ProviderName must be a valid string");

        RuleFor(x => x.Amount)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .NotEmpty()
            .WithMessage("Amount must not be null.")
            .GreaterThan(0)
            .WithMessage("Amount must be greater than zero.");

        RuleFor(x => x.ProviderAddress1)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .NotEmpty()
            .WithMessage("ProviderAddress1 must not be empty or null.")
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .MaximumLength(50)
            .WithMessage("Provider Address1 must be a valid address");

        RuleFor(x => x.ProviderAddress2)
           .MaximumLength(50)
           .When(x => !string.IsNullOrWhiteSpace(x.ProviderAddress2));

        RuleFor(x => x.State)
          .Cascade(CascadeMode.Stop)
          .NotNull()
          .NotEmpty()
          .WithMessage("State must not be empty or null.")
          .Must(value => !string.IsNullOrWhiteSpace(value))
          .MaximumLength(32)
          .WithMessage("State must be a valid string");

        RuleFor(x => x.City)
          .Cascade(CascadeMode.Stop)
          .NotNull()
          .NotEmpty()
          .WithMessage("City must not be empty or null.")
          .Must(value => !string.IsNullOrWhiteSpace(value))
          .MaximumLength(64);


        RuleFor(x => x.ZipCode)
          .Cascade(CascadeMode.Stop)
          .NotEmpty()
          .WithMessage("ZipCode must not be empty or null.")
          .Must(value => !string.IsNullOrWhiteSpace(value))
          .Matches("^[0-9]{5}$")
          .WithMessage("ZipCode must be a valid 5-digit number.");

        RuleFor(x => x.Plus4)
           .Matches("^[0-9]{4}$")
           .When(x => !string.IsNullOrWhiteSpace(x.Plus4)) // Applies validation only if Plus4 is not null or empty
           .WithMessage("Plus4 must be a valid 4-digit number.");
    }
}
