using FluentValidation;

namespace VPay.Optum.PaymentRouting.Api.Controllers.Models.Validators
{
    public class QueuePaymentRecordBodyValidator : AbstractValidator<QueuePaymentRecordBody>
    {
        public QueuePaymentRecordBodyValidator() =>
            RuleFor(x => x.PaymentRecord)
                .NotEmpty();
    }
}
