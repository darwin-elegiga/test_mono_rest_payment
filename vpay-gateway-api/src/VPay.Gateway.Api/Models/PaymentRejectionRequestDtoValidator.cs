using FluentValidation;
using VPay.Gateway.Common;
using VPay.Gateway.Common.Preferencing.Enums;

namespace VPay.Gateway.Api.Models;

public class PaymentRejectionRequestDtoValidator : AbstractValidator<PaymentRejectionRequestDto>
{
    public PaymentRejectionRequestDtoValidator()
    {
        RuleFor(x => x.NetworkPartner)
            .NotEmpty()
            .WithMessage("Required field missing - {PropertyName} must be provided")
            .MaximumLength(20)
            .WithMessage("Invalid field format - {PropertyName} must not exceed 20 characters");

        RuleFor(x => x.OptumTransactionId)
            .NotEmpty()
            .WithMessage("Required field missing - {PropertyName} must be provided")
            .GreaterThan(0)
            .WithMessage("Invalid field format - {PropertyName} must be a non-zero positive number");

        RuleFor(x => x.RejectionReasonCode)
            .NotEmpty()
            .WithMessage("{Required field missing - PropertyName} must be provided")
            .MaximumLength(4)
            .WithMessage("Invalid field format - {PropertyName} must not exceed 4 characters")
            .Must(BeAlphaNumericCode)
            .WithMessage("Unrecognized rejection reason code - {PropertyName} must be a valid payment rejection code")
            .Must(BeValidRejectionCode)
            .WithMessage("Unrecognized rejection reason code - {PropertyName} must be a valid payment rejection code");

        RuleFor(x => x.RejectionReason)
            .NotEmpty()
            .WithMessage("Required field missing - {PropertyName} must be provided")
            .MaximumLength(256)
            .WithMessage("Invalid field format - {PropertyName} must not exceed 256 characters");
    }

    private static bool BeAlphaNumericCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }
        return value.All(x => char.IsLetterOrDigit(x)) && !value.All(x => char.IsDigit(x));
    }

    private static bool BeValidRejectionCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }
        return value.ToEnum(NetworkPaymentRejectionCode.None) != NetworkPaymentRejectionCode.None;
    }
}
