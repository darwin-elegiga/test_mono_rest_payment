using System.Collections.Generic;
using System.ComponentModel;
using FluentValidation;
using FluentValidation.Results;

namespace VPay.DocSys.Parsing.Common
{
    public class StpPaymentGroupDto
    {
        /// <summary>
        /// The name of the network partner.
        /// </summary>
        [DefaultValue("")]
        public string NetworkName { get; set; } = "";

        /// <summary>
        /// The network enrollment type: B2B or C2B.
        /// </summary>
        [DefaultValue("B2B")]
        public string EnrollType { get; set; } = "B2B";

        /// <summary>
        /// The EDI 835 type: PRS or SDS.
        /// </summary>
        [DefaultValue("SDS")]
        public string Edi835Type { get; set; } = "SDS";

        /// <summary>
        /// The flag indicating whether the remittance is valid.
        /// </summary>
        [DefaultValue(false)]
        public bool RemitValid { get; set; } = false;

        /// <summary>
        /// The flag indicating whether an EDI 835 file is available.
        /// </summary>
        [DefaultValue(false)]
        public bool Edi835Avail { get; set; } = false;

        /// <summary>
        /// The flag indicating whether the payment has been sent.
        /// </summary>
        [DefaultValue(false)]
        public bool PaymentSent { get; set; } = false;

        /// <summary>
        /// The flag indicating whether the PMB message has been sent.
        /// </summary>
        [DefaultValue(false)]
        public bool PmbMessageSent { get; set; } = false;

        /// <summary>
        /// The list of transaction IDs associated with the payment group.
        /// </summary>
        public IEnumerable<long> TransactionIds { get; set; } = new List<long>();

        public ValidationResult Validate()
        {
            var validator = new StpPaymentGroupDtoValidator();
            var result = validator.Validate(this);
            return result;
        }
    }

    public class StpPaymentGroupDtoValidator : AbstractValidator<StpPaymentGroupDto>
    {
        public StpPaymentGroupDtoValidator()
        {
            RuleFor(x => x.NetworkName)
                .MaximumLength(20).WithMessage("Network partner must not exceed 20 characters.")
                .NotEmpty().WithMessage("Network name is required.");

            RuleFor(x => x.EnrollType)
                .MaximumLength(3).WithMessage("Enrollment type must not exceed 3 characters.")
                .NotEmpty().WithMessage("Enrollment type is required.")
                .Must(BeAValidEnrollType).WithMessage("Invalid enrollment type.");

            RuleFor(x => x.Edi835Type)
                .MaximumLength(3).WithMessage("Enrollment type must not exceed 3 characters.")
                .NotEmpty().WithMessage("EDI 835 type is required.")
                .Must(BeAValidEdi835Type).WithMessage("Invalid EDI 835 type.");

            RuleFor(x => x.TransactionIds)
                .NotEmpty().WithMessage("At least one transaction ID is required.");
            
            RuleForEach(x => x.TransactionIds)
                .GreaterThan(0).WithMessage("Transaction IDs must be positive numbers.");
        }

        private bool BeAValidEnrollType(string enrollType)
        {
            return enrollType == "B2B" || enrollType == "C2B";
        }

        private bool BeAValidEdi835Type(string edi835Type)
        {
            return edi835Type == "SDS" || edi835Type == "PRS";
        }
    }
}
