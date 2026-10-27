using FluentValidation;
using VPay.Optum.PaymentRouting.Common.Serialization;

namespace VPay.Optum.PaymentRouting.Api.Controllers.Models.Validators
{
    public class PaymentSummaryValidator : AbstractValidator<PaymentSummary>
    {
        public PaymentSummaryValidator()
        {
            RuleFor(x => x.BatchId).NotNull().NotEmpty().MaximumLength(49);
            RuleFor(x => x.Payer835Id).NotNull().NotEmpty().MaximumLength(100);
            RuleFor(x => x.TotalPaymentCount).NotNull().NotEmpty().Custom((x, context) =>
            {
                if ((!(int.TryParse(x, out int value)) || value < 0))
                {
                    context.AddFailure("Total Payment Count is not a valid number or less than 0");
                }
            });
            RuleFor(x => x.TotalPaymentAmount).Custom((x, context) =>
            {
                if (!double.TryParse(x, out double value) || value < 0)
                {
                    context.AddFailure("Total Payment Amount is not a valid number or less than 0");
                }
            });
        }
    }
}
