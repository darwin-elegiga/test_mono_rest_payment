using FluentValidation;
using FluentValidation.Results;

namespace VPay.DocSys.Parsing.Common
{
    public class PrsConfigurationDto
    {
        public string OriginCode { get; set; }

        public string ClientCode { get; set; }

        public string BillingEntityCode { get; set; }

        public bool IsPrsEnabled { get; set; }

        public ValidationResult Validate()
        {
            var validator = new PrsConfigDtoValidator();
            var result = validator.Validate(this);
            return result;
        }
    }

    public class PrsConfigDtoValidator : AbstractValidator<PrsConfigurationDto>
    {
        public PrsConfigDtoValidator()
        {
            RuleFor(x => x.OriginCode)
                .NotEmpty().WithMessage("The origin code must be provided.")
                .MaximumLength(10).WithMessage("The origin code must not exceed 10 characters.");

            RuleFor(x => x.ClientCode)                
                .NotEmpty().WithMessage("The client code must be provided.")
                .MaximumLength(10).WithMessage("The client code must not exceed 10 characters.");

            RuleFor(x => x.BillingEntityCode)
                .NotEmpty().WithMessage("The billing entity code must be provided.")
                .MaximumLength(20).WithMessage("The billing entity code must not exceed 20 characters.");
        }
    }
}
