using FluentValidation;

namespace VPay.Gateway.Api.Models;

public class TransactionSearchRequestVendorDtoValidator : AbstractValidator<TransactionSearchRequestVendorDto>
{
    public TransactionSearchRequestVendorDtoValidator()
    {
        RuleFor(x => x.PayerCode)
            .NotEmpty().WithMessage("PayerCode is required.")
            .MaximumLength(20).WithMessage("PayerCode must not exceed 20 characters.");

        RuleFor(x => x.SubPayerCode)
            .NotEmpty().WithMessage("SubPayerCode is required.")
            .MaximumLength(20).WithMessage("SubPayerCode must not exceed 20 characters.");

        RuleFor(x => x.PreTransactionId)
            .NotEmpty().When(x => x.TransactionId == 0)
            .WithMessage("PreTransactionId is required if TransactionId is not provided.")
            .MaximumLength(30).WithMessage("PreTransactionId must not exceed 30 characters.");

        RuleFor(x => x.TransactionId)
            .NotEmpty().When(x => string.IsNullOrWhiteSpace(x.PreTransactionId))
            .WithMessage("TransactionId is required if PreTransactionId is not provided.");

        RuleFor(x => x)
            .Must(dto => !string.IsNullOrWhiteSpace(dto.PreTransactionId) || dto.TransactionId != 0)
            .WithMessage("Either PreTransactionId or TransactionId must be provided.");
    }
}
