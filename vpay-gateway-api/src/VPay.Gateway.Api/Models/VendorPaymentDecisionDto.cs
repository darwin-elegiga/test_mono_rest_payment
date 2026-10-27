using FluentValidation;
using VPay.Gateway.Common;
using VPay.Gateway.Common.MemberClaims.Models;

namespace VPay.Gateway.Api.Models;

/// <summary>
/// Represents a request for vendor payment decision.
/// </summary>
public class VendorPaymentDecisionDto
{
    /// <summary>
    /// The Client Code. Required.
    /// </summary>
    public string PayerCode { get; set; }

    /// <summary>
    /// The Billing Entity Code. Required.
    /// </summary>
    public string SubpayerCode { get; set; }

    /// <summary>
    /// The vendor unique Pre-Transaction Id. Required.
    /// </summary>
    public string PreTransId { get; set; }

    /// <summary>
    /// The list of members for the payment. Required, at least one member must be provided,
    /// but no more than one member can be provided for (single-member payments).
    /// </summary>
    public IList<VendorPaymentDecisionMemberDto> MemberIDs { get; set; } = Array.Empty<VendorPaymentDecisionMemberDto>();

    /// <summary>
    /// The payment type selected for the payment (Check, ACH/EFT, or Digital Card) if the claims were accepted,
    /// or Rejection if payment claims were not accepted. Required.
    /// </summary>
    public string DecisionedModality { get; set; }

    /// <summary>
    /// The flag to determine if a selection has expired or not. Required.
    /// </summary>
    public bool NoSelectionExpiredIndicator { get; set; }

    /// <summary>
    /// The reason for the rejection of the payment claims. Optional, required if Decision Modality is 'PREISREJ'
    /// </summary>
    public string ClaimRejectionReason { get; set; }

    /// <summary>
    /// The vendor unique Transaction Id. Required.
    /// </summary>
    public string ZetaTransactionId { get; set; }
}

public class VendorPaymentDecisionDtoValidator : AbstractValidator<VendorPaymentDecisionDto>
{
    public VendorPaymentDecisionDtoValidator()
    {
        // Required, should not exceed max length
        RuleFor(x => x.PayerCode)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .MaximumLength(10).WithMessage("{PropertyName} must not exceed {MaxLength} characters.");

        // Required, should not exceed max length
        RuleFor(x => x.SubpayerCode)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .MaximumLength(20).WithMessage("{PropertyName} must not exceed {MaxLength} characters.");

        // Required, should not exceed max length
        RuleFor(x => x.PreTransId)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .MaximumLength(50).WithMessage("{PropertyName} must not exceed {MaxLength} characters.");

        // Required, must contain at least one member but no more than max members.
        // NOTE: Today max members is set to 1 for single-member payments,
        // but this can be increased in the future for multi-member payments,
        // so the validation should be updated accordingly when that happens.
        RuleFor(x => x.MemberIDs)
            .NotEmpty().WithMessage("{PropertyName} must contain at least one member.")
            .Must(x => x.Count <= 1).WithMessage("{PropertyName} must not contain more than 1 member(s).");

        // Required, should not exceed max length, must be a valid modality value:
        // PREISCHK (Check)
        // PREISDCP (Digital Card)
        // PREISEFT (ACH/EFT)
        // PREISREJ (Rejection; Pre-Transaction Void)
        RuleFor(x => x.DecisionedModality)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .MaximumLength(8).WithMessage("{PropertyName} must not exceed {MaxLength} characters.")
            .Must(VendorPaymentDecisionModality.IsValid).WithMessage("{PropertyName} is not valid ({PropertyValue}).");

        // Only required if Decision Modality is 'PREISREJ'
        When(x => x.DecisionedModality == VendorPaymentDecisionModality.PreTransactionIsRejectedClaim.Name, () =>
        {
            RuleFor(x => x.ClaimRejectionReason)
                .NotEmpty().WithMessage("{PropertyName} is required when Decision Modality is 'PREISREJ'.")
                .MaximumLength(100).WithMessage("{PropertyName} must not exceed {MaxLength} characters.");
        });

        // Required, should not exceed max length
        RuleFor(x => x.ZetaTransactionId)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .MaximumLength(50).WithMessage("{PropertyName} must not exceed {MaxLength} characters.");

        #region Child Validation

        RuleForEach(x => x.MemberIDs)
            .SetValidator(x => new VendorPaymentDecisionMemberDtoValidator(x));

        #endregion
    }
}

public static class VendorPaymentDecisionDtoMapper
{
    public static VendorPaymentDecision FromDto(this VendorPaymentDecisionDto item)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(item.MemberIDs);
        return new VendorPaymentDecision
        {
            PayerCode = item.PayerCode,
            SubpayerCode = item.SubpayerCode,
            PreTransId = item.PreTransId,
            DecisionedModality = item.DecisionedModality,
            NoSelectionExpiredIndicator = item.NoSelectionExpiredIndicator,
            ClaimRejectionReason = item.ClaimRejectionReason,
            ZetaTransactionId = item.ZetaTransactionId,
            MemberIDs = [..item.MemberIDs.Select(m => m.FromDto())]
        };
    }

    public static IEnumerable<VendorPaymentDecision> FromDtoList(this IEnumerable<VendorPaymentDecisionDto> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        foreach (var item in items)
        {
            yield return FromDto(item);
        }
    }
}
