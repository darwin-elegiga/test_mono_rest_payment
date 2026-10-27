using FluentValidation;
using VPay.DocSys.Common;

namespace VPay.DocSys.Generation.Api.Validators
{
    public class MergePdfParamValidator : AbstractValidator<MergePdfParam>
    {

        public MergePdfParamValidator()
        {
            RuleFor(x => x.OutputFileName).NotEmpty();
            RuleFor(x => x.Files).NotEmpty();
        }
    }
}
