using FluentValidation;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;

namespace VPay.DocSys.Generation.Api.Validators
{
    public class DocGenParamValidator : AbstractValidator<DocGenParam>
    {

        public DocGenParamValidator()
        {
            RuleFor(x => x.StatusRecord).NotNull();
            RuleFor(x => x.Configuration).NotNull();
            RuleFor(x => x.EOBData).NotNull().When(x => x.GenerateEOB);
            RuleFor(x => x.OtbRecord).NotNull();

            RuleFor(x => x.SubstituteDataInfo).NotNull().When(x => x.StatusRecord != null && x.StatusRecord.DIST == DistType.SendBack)
                    .WithMessage("'{PropertyName}' must not be empty when Status Record Dist = '" + DistType.SendBack + "'");
        }

    }
}
