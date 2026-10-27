namespace VPay.Gateway.ClientProcessor.Rabbit;

/// <summary>
/// This class contains all of the MEP claim statuses we care about.
/// NOTE: This is not necessarily an exhaustive list of statuses.
/// </summary>
public static class MepEnrollmentClaimStatuses
{
    /// <summary>
    /// Enrollment request received from client.
    /// </summary>
    public const string EnrollmentReceived = "ENRLRCVD";

    /// <summary>
    /// Enrollment workflow is in progress.  Payee/resolution will be determined based on actions of parties.
    /// </summary>
    public const string EnrollmentInProgress = "ENRLINPROG";

    /// <summary>
    /// EFT enrollment successful.
    /// </summary>
    public const string EnrollmentSuccess = "ENRLISEFT";

    /// <summary>
    /// EFT enrollment link has expired.
    /// </summary>
    public const string EnrollmentExpired = "ENRLEXP";

    /// <summary>
    /// EFT Enrollment failed (too many failed validation/auth attempts).
    /// </summary>
    public const string EnrollmentFailed = "ENRLFAIL";
}
