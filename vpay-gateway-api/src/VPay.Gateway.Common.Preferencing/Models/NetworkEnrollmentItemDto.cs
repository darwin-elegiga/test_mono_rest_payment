using System.Diagnostics.CodeAnalysis;
using FluentValidation;
using VPay.Gateway.Common.Preferencing.Enums;

namespace VPay.Gateway.Common.Preferencing.Models;

[ExcludeFromCodeCoverage]
public class NetworkEnrollmentItemDto
{
    /// <summary>
    /// The national provider identification number.
    /// </summary>
    public string? Npi { get; set; }

    /// <summary>
    /// The provider address street line 1.
    /// </summary>
    public string Address1 { get; set; } = string.Empty;

    /// <summary>
    /// The provider address street line 2.
    /// </summary>
    public string? Address2 { get; set; }

    /// <summary>
    /// The provider address city.
    /// </summary>
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// The provider address state.
    /// </summary>
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// The provider address zip code.
    /// </summary>
    public string Zip9 { get; set; } = string.Empty;

    /// <summary>
    /// The enrollment type: C2B or B2B.
    /// </summary>
    public string EnrollmentType { get; set; } = string.Empty;

    /// <summary>
    /// The enrollment enabled/disabled status code: Y/N.
    /// </summary>
    public string EnrollmentEligibilityStatus { get; set; } = string.Empty;

    /// <summary>
    /// The enrollment effective or start date.
    /// </summary>
    public DateTime EffectiveDate { get; set; }

    /// <summary>
    /// The initial enrollment effective or start date.
    /// </summary>
    public DateTime InitialEnrollmentDate { get; set; }

    /// <summary>
    /// The last modified date/time in UTC.
    /// </summary>
    public DateTime LastUpdatedUtcDate { get; set; }

    /// <summary>
    /// The enrollment active/inactive status code:A/I.
    /// </summary>
    public string EnrollmentStatusCd { get; set; } = string.Empty;

    /// <summary>
    /// The enrollment reason code.
    /// </summary>
    public string? EnrollmentStatusReason { get; set; }

    /// <summary>
    /// The enrollment UUID
    /// </summary>
    public string? Uuid { get; set; }


}

public class NetworkEnrollmentItemDtoValidator : AbstractValidator<NetworkEnrollmentItemDto>
{
    public NetworkEnrollmentItemDtoValidator(NetworkEnrollmentListDto tin)
    {
        RuleFor(x => x.Npi)
            .Matches("^\\d{1,10}$").When(y => !string.IsNullOrWhiteSpace(y.Npi))
            .WithMessage("{PropertyName} must be a 1 to 10-digit number");

        RuleFor(x => x.Address1)
            .NotEmpty()
            .WithMessage(x => Adorn(tin.ProviderTin, nameof(x.Address1), "must be provided"))
            .MaximumLength(50)
            .WithMessage(x => Adorn(tin.ProviderTin, nameof(x.Address1), "must not exceed 50 characters"));

        RuleFor(x => x.Address2)
            .MaximumLength(50)
            .WithMessage(x => Adorn(tin.ProviderTin, nameof(x.Address2), "must not exceed 50 characters"));

        RuleFor(x => x.City)
            .NotEmpty()
            .WithMessage(x => Adorn(tin.ProviderTin, nameof(x.City), "must be provided"))
            .MaximumLength(30)
            .WithMessage(x => Adorn(tin.ProviderTin, nameof(x.City), "must not exceed 30 characters"));

        RuleFor(x => x.State)
            .NotEmpty()
            .WithMessage(x => Adorn(tin.ProviderTin, nameof(x.State), "must be provided"))
            .MaximumLength(2)
            .WithMessage(x => Adorn(tin.ProviderTin, nameof(x.State), "must not exceed 2 characters"));

        RuleFor(x => x.Zip9)
            .NotEmpty()
            .WithMessage(x => Adorn(tin.ProviderTin, nameof(x.Zip9), "must be provided"))
            .Matches("^\\d{5}(-?\\d{4})?$")
            .WithMessage(x => Adorn(tin.ProviderTin, nameof(x.Zip9), "must be valid zip code or zip code + 4"));

        RuleFor(x => x.EnrollmentType)
            .NotEmpty()
            .WithMessage(x => Adorn(tin.ProviderTin, nameof(x.EnrollmentType), "must be provided"))
            .MaximumLength(5)
            .WithMessage(x => Adorn(tin.ProviderTin, nameof(x.EnrollmentType), "must not exceed 5 characters"))
            .Must(x => x?.ToEnum(NetworkEnrollmentType.None) != NetworkEnrollmentType.None)
            .WithMessage(x => Adorn(tin.ProviderTin, nameof(x.EnrollmentType), "must be a valid enrollment type"));

        RuleFor(x => x.EnrollmentEligibilityStatus)
            .NotEmpty()
            .WithMessage(x => Adorn(tin.ProviderTin, nameof(x.EnrollmentEligibilityStatus), "must be provided"))
            .MaximumLength(1)
            .WithMessage(x => Adorn(tin.ProviderTin, nameof(x.EnrollmentEligibilityStatus), "must not exceed 1 character"))
            .Must(x => x?.ToEnum(NetworkEnrollmentEnabledStatus.None) != NetworkEnrollmentEnabledStatus.None)
            .WithMessage(x => Adorn(tin.ProviderTin, nameof(x.EnrollmentEligibilityStatus), "must be a valid eligibility status"));

        RuleFor(x => x.EffectiveDate)
            .NotEmpty()
            .WithMessage(x => Adorn(tin.ProviderTin, nameof(x.EffectiveDate), "must be provided"));

        RuleFor(x => x.InitialEnrollmentDate)
            .NotEmpty()
            .WithMessage(x => Adorn(tin.ProviderTin, nameof(x.InitialEnrollmentDate), "must be provided"));

        RuleFor(x => x.LastUpdatedUtcDate)
            .NotEmpty()
            .WithMessage(x => Adorn(tin.ProviderTin, nameof(x.LastUpdatedUtcDate), "must be provided"));

        RuleFor(x => x.EnrollmentStatusCd)
            .NotEmpty()
            .WithMessage(x => Adorn(tin.ProviderTin, nameof(x.EnrollmentStatusCd), "must be provided"))
            .MaximumLength(1)
            .WithMessage(x => Adorn(tin.ProviderTin, nameof(x.EnrollmentStatusCd), "must not exceed 1 character"))
            .Must(x => x?.ToEnum(NetworkEnrollmentActiveStatus.None) != NetworkEnrollmentActiveStatus.None)
            .WithMessage(x => Adorn(tin.ProviderTin, nameof(x.EnrollmentStatusCd), "must be a valid enrollment status"));

        RuleFor(x => x.EnrollmentStatusReason)
            .MaximumLength(50)
            .WithMessage(x => Adorn(tin.ProviderTin, nameof(x.EnrollmentStatusReason), "must not exceed 50 characters"));

        RuleFor(x => x.Uuid)
            .MaximumLength(50)
            .WithMessage(x => Adorn(tin.ProviderTin, nameof(x.Uuid), "must not exceed 50 characters"));
    }

    private static string Adorn(string tin, string prop, string message)
    {
        return $"[{tin}] '{prop}' {message}";
    }
}
