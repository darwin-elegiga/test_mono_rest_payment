using System.Linq;
using FluentValidation;
using VPay.Optum.PaymentRouting.Common.RabbitMessages;

namespace VPay.Optum.PaymentRouting.Api.Controllers.Models.Validators
{
    public class RemittanceAdviceFileDtoValidator : AbstractValidator<RemittanceAdviceFileMessage>
    {
        private static string[] ValidRemitMethodValues = { "PDF", "DATA", "DATA,PDF", "PDF,DATA", "NONE" };
        public RemittanceAdviceFileDtoValidator()
        {
            RuleFor(x => x.Payer835Id).NotNull().NotEmpty().MaximumLength(10);

            RuleFor(x => x.InboundZipfile).NotNull().NotEmpty().MaximumLength(512);

            RuleFor(x => x.UserKey).NotNull().NotEmpty().MaximumLength(49);

            RuleFor(x => x.PayerAlias).NotNull().NotEmpty().Length(5);

            RuleFor(x => x.OriginCode).NotNull().NotEmpty().MaximumLength(50);

            RuleFor(x => x.ClientCode).NotNull().NotEmpty().MaximumLength(10);

            RuleFor(x => x.BillingEntityCode).MaximumLength(10);

            RuleFor(x => x.RemitMethod).NotNull().NotEmpty()
                .Custom((x, context) =>
                {
                    if (!ValidRemitMethodValues.Contains(x))
                    {
                        var list = string.Join(", ", ValidRemitMethodValues.Select(_ => $"'{_}'").ToArray());
                        context.AddFailure($"RemitMethod must be one of {list}");
                    }
                });

            RuleFor(x => x.PdfExtractPath).NotNull().NotEmpty().MaximumLength(512);

            RuleFor(x => x.CreatedBy).MaximumLength(20);
        }
    }
}
