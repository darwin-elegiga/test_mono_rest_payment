using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using FluentValidation;
using FluentValidation.Results;

namespace VPay.DocSys.Parsing.Common
{
    public class StpPaymentSearchDto
    {
        /// <summary>
        /// The network name. Null for any network
        /// </summary>
        [AllowNull]
        [DefaultValue(null)]
        public string NetworkName { get; set; } = null;

        /// <summary>
        /// The network enrollment type: B2B or C2B. Null for any enrollment type.
        /// </summary>
        [AllowNull]
        [DefaultValue(null)]
        public string EnrollType { get; set; } = null;

        /// <summary>
        /// The EDI 835 type: PRS or SDS. Null for any EDI 835 type.
        /// </summary>
        [AllowNull]
        [DefaultValue(null)]
        public string Edi835Type { get; set; } = null;

        /// <summary>
        /// Filter STP payments with either a valid or invalid remittance. Null to ignore flag.
        /// </summary>
        [AllowNull]
        [DefaultValue(null)]
        public bool? RemitValid { get; set; } = null;

        /// <summary>
        /// Filter STP payments with either an available or not available 835 file. Null to ignore flag.
        /// </summary>
        [AllowNull]
        [DefaultValue(null)]
        public bool? Edi835Avail { get; set; } = null;

        /// <summary>
        /// Filter STP payments that either have or have not been sent. Null to ignore flag.
        /// </summary>
        [AllowNull]
        [DefaultValue(null)]
        public bool? PaymentSent { get; set; } = null;

        /// <summary>
        /// Filter STP payments that either have or have not had the PMB message sent. Null to ignore flag.
        /// </summary>
        [AllowNull]
        [DefaultValue(null)]
        public bool? PmbMessageSent { get; set; } = null;

        /// <summary>
        /// Filter STP payments based on the number of times the payment has been retried. Null to ignore.
        /// </summary>
        [AllowNull]
        [DefaultValue(null)]
        public int? PaymentSentRetries { get; set; } = null;

        /// <summary>
        /// The maximum number of results to return. The default is 10,000.
        /// </summary>
        [DefaultValue(10000)]
        public int MaxResults { get; set; } = 10000;

        public ValidationResult Validate()
        {
            var validator = new StpPaymentSearchDtoValidator();
            var result = validator.Validate(this);
            return result;
        }
    }

    public class StpPaymentSearchDtoValidator : AbstractValidator<StpPaymentSearchDto>
    {
        public StpPaymentSearchDtoValidator()
        {
            RuleFor(x => x.NetworkName)
                .MaximumLength(20).WithMessage("Network partner must not exceed 20 characters.");
            RuleFor(x => x.EnrollType)
                .MaximumLength(3).WithMessage("Enrollment type must not exceed 3 characters.");
            RuleFor(x => x.Edi835Type)
                .MaximumLength(3).WithMessage("Edi 835 Type must not exceed 3 characters.");
            RuleFor(x => x.PaymentSentRetries)                
                .GreaterThanOrEqualTo(0).When(y => y.PaymentSentRetries != null).WithMessage("Payment Sent Retries must be greater than or equal to 0.");
            RuleFor(x => x.MaxResults)
                .InclusiveBetween(1, 10000).WithMessage("Max Results must be between 1 and 10,000.");
        }
    }
}
