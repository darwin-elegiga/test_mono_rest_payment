namespace VPay.Gateway.ClientProcessor.Rabbit;

/// <summary>
/// This class contains all of the MEP party statuses we care about.
/// NOTE: This is not necessarily an exhaustive list of statuses.
/// </summary>
public static class MepEnrollmentPartyStatuses
{
    /// <summary>
    /// Unable to queue email for sending.
    /// </summary>
    public const string UnableToSendEmail = "ENRLEMRQFL";

    /// <summary>
    /// Email was returned as not deliverable.
    /// </summary>
    public const string EmailNotDeliverable = "ENRLEMFAIL";

    /// <summary>
    /// EFT Enrollment link has expired.
    /// </summary>
    public const string EnrollmentLinkExpired = "ENRLEXP";

    /// <summary>
    /// Party Details have been rejected.
    /// </summary>
    public const string PartyDetailsRejected = "ENRLDETREJ";

    /// <summary>
    /// EFT Enrollment failed (too many failed validation/auth attempts).
    /// </summary>
    public const string EnrollmentFailed = "ENRLFAIL";

    /// <summary>
    /// Party unable to provide valid EFT details.
    /// </summary>
    public const string InvalidEftInformation = "ENRLEFTREJ";

    /// <summary>
    /// Party unable to successfully complete identity verification.
    /// </summary>
    public const string IdentityVerificationUnsuccessful = "ENRLIDVERREJ";

    /// <summary>
    /// Potential match for this party was found on a sanctions list.
    /// </summary>
    public const string PotentiallySanctioned = "ENRLOFACFL";

    /// <summary>
    /// Party has rejected Terms and Conditions for chosen payment method.
    /// </summary>
    public const string TermsAndConditionsRejected = "ENRLTCREJ";
}
