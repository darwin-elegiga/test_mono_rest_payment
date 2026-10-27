using FluentValidation;
using VPay.Gateway.Common;
using VPay.Gateway.Common.MemberClaims.Models;

namespace VPay.Gateway.Api.Models;

/// <summary>
/// Represents a member ID entry.
/// </summary>
public class VendorPaymentDecisionMemberDto
{
    /// <summary>
    /// The vendor member Id. Required.
    /// </summary>
    public string ZetaMemberId { get; set; }

    /// <summary>
    /// The client member Id. Required.
    /// </summary>
    public string ClientMemberId { get; set; }

    /// <summary>
    /// The internal Optum member Id. Optional, required if Decision Modality is 'PREISEFT'
    /// </summary>
    public string OptumMemberId { get; set; }

    /// <summary>
    /// The party Id. Required.
    /// </summary>
    public string PartyId { get; set; }
}

public class VendorPaymentDecisionMemberDtoValidator : AbstractValidator<VendorPaymentDecisionMemberDto>
{
    public VendorPaymentDecisionMemberDtoValidator(VendorPaymentDecisionDto decision)
    {
        // Required, should not exceed max length
        RuleFor(x => x.ZetaMemberId)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .MaximumLength(50).WithMessage("{PropertyName} must not exceed {MaxLength} characters.");

        // Required, should not exceed max length
        RuleFor(x => x.ClientMemberId)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .MaximumLength(50).WithMessage("{PropertyName} must not exceed {MaxLength} characters.");

        // Optional, required only if Decision Modality is 'PREISEFT', should be greater than zero if provided
        When(_ => decision.DecisionedModality == VendorPaymentDecisionModality.PreTransactionIsAchOrEft.Name, () =>
        {
            RuleFor(x => x.OptumMemberId)
                .NotEmpty().WithMessage("{PropertyName} is required when Decision Modality is 'PREISEFT'.")
                .MaximumLength(20).WithMessage("{PropertyName} must not exceed {MaxLength} characters.")
                .Must(BeNumericAndGreaterThanZero).WithMessage("{PropertyName} must be a valid numeric value.");
        });

        // Required, should not exceed max length
        // NOTE: Should be numeric value in the future when we have the Party ID format defined,
        // but currently we will just validate the length and required constraint until Member Claims API is fixed.
        RuleFor(x => x.PartyId)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .MaximumLength(20).WithMessage("{PropertyName} must not exceed {MaxLength} characters.")
            .Must(BeNumericAndGreaterThanZero).WithMessage("{PropertyName} must be a valid numeric value.");
    }

    private static bool BeNumericAndGreaterThanZero(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }
        return long.TryParse(value, out var result) && result > 0;
    }
}

public static class VendorPaymentDecisionMemberDtoSanitizer
{
    /// <summary>
    /// Removes control characters, new line characters, or other non-printable characters.
    /// </summary>
    /// <param name="member">The entity to sanitize.</param>
    /// <returns>A clean copy of the provided data without any control characters or non-printable characters.</returns>
    public static VendorPaymentDecisionMemberDto? Sanitize(this VendorPaymentDecisionMemberDto member)
    {
        if (member == null)
        {
            return null;
        }
        return new VendorPaymentDecisionMemberDto
        {
            ZetaMemberId = member.ZetaMemberId.Sanitize(),
            ClientMemberId = member.ClientMemberId.Sanitize(),
            OptumMemberId = member.OptumMemberId,
            PartyId = member.PartyId
        };
    }

    /// <summary>
    /// Remove control characters, new line characters, or other non-printable characters,
    /// and replaces null values for suitable values to log.
    /// </summary>
    /// <param name="member">The entity to sanitize.</param>
    /// <returns>A clean copy of the provided data without any control characters or non-printable characters.</returns>
    public static VendorPaymentDecisionMemberDto? SanitizeLog(this VendorPaymentDecisionMemberDto member)
    {
        if (member == null)
        {
            return null;
        }
        return new VendorPaymentDecisionMemberDto
        {
            ZetaMemberId = member.ZetaMemberId.Fix(nullValue: "NULL", emptyValue: ""),
            ClientMemberId = member.ClientMemberId.Fix(nullValue: "NULL", emptyValue: ""),
            OptumMemberId = member.OptumMemberId,
            PartyId = member.PartyId
        };
    }
}

public static class VendorPaymentDecisionMemberDtoMapper
{
    public static VendorPaymentDecisionMember FromDto(this VendorPaymentDecisionMemberDto item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return new VendorPaymentDecisionMember
        {
            ZetaMemberId = item.ZetaMemberId,
            ClientMemberId = item.ClientMemberId,
            OptumMemberId = item.OptumMemberId,
            PartyId = item.PartyId
        };
    }

    public static IEnumerable<VendorPaymentDecisionMember> FromDtoList(this IEnumerable<VendorPaymentDecisionMemberDto> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        foreach (var item in items)
        {
            yield return FromDto(item);
        }
    }
}
