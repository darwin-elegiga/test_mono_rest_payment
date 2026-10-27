using FluentValidation;

namespace VPay.Gateway.Api.Models;

public class CreateEnrollmentDtoValidator : AbstractValidator<CreateEnrollmentDto>
{
    public CreateEnrollmentDtoValidator()
    {
        RuleFor(x => x.FundingAccountCode)
            .NotEmpty();

        RuleFor(x => x.PayeeName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.PayeeEmail)
            .NotEmpty()
            .MaximumLength(100)
            .Matches(@"(^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+$)");

        RuleFor(x => x.MemberId)
            .NotEmpty()
            .MaximumLength(20);

        RuleFor(x => x.PolicyNumber)
            .MaximumLength(100);

        Transform(from: x => x.CellPhoneNumber, to: value => value?.Replace("(", "").Replace(")", ""))
            .NotEmpty()
            .MaximumLength(20)
            .Matches(@"^\s*(\+\s*1|1)?\s*[-/\.]?\s*\d{3}\s*[-/\.]?\s*\d{3}\s*[-/\.]?\s*\d{4}\s*");
    }
}
