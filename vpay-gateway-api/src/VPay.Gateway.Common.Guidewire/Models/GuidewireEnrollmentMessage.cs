namespace VPay.Gateway.Common.Guidewire.Models;

public sealed class GuidewireEnrollmentMessage
{
    public const string Scheduled = "SCHEDULED";
    public const string Started = "STARTED";
    public const string Skipped = "SKIPPED";
    public const string Sending = "SENDING";
    public const string Failed = "FAILED";
    public const string Completed = "COMPLETED";
    public const string MarkedCompleted = "MARKCOMPLETED";
}
