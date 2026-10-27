using FluentValidation;
using VPay.DocSys.Common;

namespace VPay.DocSys.Generation.Api.Validators
{
    public class StampPdfParamValidator : AbstractValidator<StampPdfParam>
    {
        public StampPdfParamValidator()
        {
            RuleFor(x => x.OutputFileName).NotEmpty();
            RuleFor(x => x.FileName).NotEmpty();
            RuleFor(x => x.StampFileName).NotEmpty();
        }
    }
}
