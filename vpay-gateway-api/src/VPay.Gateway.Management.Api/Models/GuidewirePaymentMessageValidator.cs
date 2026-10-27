using FluentValidation;

namespace VPay.Gateway.Management.Api.Models;

public class GuidewirePaymentMessageValidator : AbstractValidator<GuidewirePaymentMessageDto>
{
    public GuidewirePaymentMessageValidator()
    {
        RuleFor(x => x.ClientCode)
            .NotEmpty()
            .MaximumLength(10);

        RuleFor(x => x.PaymentId)
            .NotEmpty();

        RuleFor(x => x.Status)
            .NotEmpty();

        RuleFor(x => x.Note)
            .NotEmpty()
            .MaximumLength(255);
    }
}
