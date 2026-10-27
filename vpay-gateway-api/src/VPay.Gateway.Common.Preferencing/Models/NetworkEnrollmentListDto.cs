using FluentValidation;

namespace VPay.Gateway.Common.Preferencing.Models;

public class NetworkEnrollmentListDto
{
    /// <summary>
    /// The provider tax id number.
    /// </summary>
    public string ProviderTin { get; set; } = string.Empty;

    /// <summary>
    /// The provider legal entity name.
    /// </summary>
    public string LegalEntityName { get; set; } = string.Empty;

    /// <summary>
    /// The provider doing business as name.
    /// </summary>
    public string? Dba { get; set; }

    /// <summary>
    /// The provider enrollment list including entries for each enrollment type (B2B, C2B).
    /// </summary>
    public IEnumerable<NetworkEnrollmentItemDto> EnrollmentList { get; set; } = new List<NetworkEnrollmentItemDto>();
}

public class NetworkEnrollmentListDtoValidator : AbstractValidator<NetworkEnrollmentListDto>
{
    public NetworkEnrollmentListDtoValidator()
    {
        RuleFor(x => x.ProviderTin)
            .NotEmpty()
            .WithMessage(x => Adorn(x.ProviderTin, nameof(x.ProviderTin), "must be provided"))
            .Matches("^[0-9]{9}$")
            .WithMessage(x => Adorn(x.ProviderTin, nameof(x.ProviderTin), "must be a 9-digit number"));

        RuleFor(x => x.LegalEntityName)
            .NotEmpty()
            .WithMessage(x => Adorn(x.ProviderTin, nameof(x.LegalEntityName), "must be provided"))
            .MaximumLength(120)
            .WithMessage(x => Adorn(x.ProviderTin, nameof(x.LegalEntityName), "must not exceed 120 characters"));

        RuleFor(x => x.Dba)
            .MaximumLength(120)
            .WithMessage(x => Adorn(x.ProviderTin, nameof(x.Dba), "must not exceed 120 characters"));

        RuleFor(x => x.EnrollmentList)
            .NotEmpty()
            .WithMessage(x => Adorn(x.ProviderTin, nameof(x.EnrollmentList), "must be provided"))
            .Must(y => y?.Count() <= 2000)
            .WithMessage(x => Adorn(x.ProviderTin, nameof(x.EnrollmentList), "must not exceed 2000 items"));

        #region Child Validation

        RuleForEach(x => x.EnrollmentList)
            .SetValidator(x => new NetworkEnrollmentItemDtoValidator(x));

        #endregion
    }

    private static string Adorn(string tin, string prop, string message)
    {
        return $"[{tin}] '{prop}' {message}";
    }
}
