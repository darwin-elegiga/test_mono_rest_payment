using FluentValidation;
using Vpay.Gateway.Common.Vpay.Models;

namespace VPay.Gateway.Api.Models;
public class TransactionSearchDtoValidator : AbstractValidator<TransactionSearchDto>
{
    public TransactionSearchDtoValidator()
    {
        RuleFor(x => x.PaymentId)
            .NotEmpty().WithMessage("{PropertyName} must be provided")
            .MaximumLength(49).WithMessage("{PropertyName} must not exceed 49 characters");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("{PropertyName} must be provided");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("{PropertyName} must be provided");

        When(x => x.StartDate != null && x.EndDate != null, () =>
        {
            RuleFor(x => x.EndDate)
              .GreaterThanOrEqualTo(r => r.StartDate)
              .WithMessage("Start Date must be before {PropertyName}");
        });

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .LessThanOrEqualTo(10000).WithMessage("{PropertyName} must be between 1 and 10000");

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1)
            .LessThanOrEqualTo(1000).WithMessage("{PropertyName} must be between 1 and 1000");

        RuleFor(x => x.ProviderTin)
              .MaximumLength(9).WithMessage("{PropertyName} must not exceed 9 characters");
    }
}
