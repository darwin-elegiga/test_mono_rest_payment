using Ardalis.SmartEnum;

namespace VPay.Gateway.ClientProcessor.Rabbit;

public sealed class EnrollmentNotificationStatus : SmartEnum<EnrollmentNotificationStatus>
{
    public static readonly EnrollmentNotificationStatus Scheduled = new EnrollmentNotificationStatus("SCHEDULED", 0);
    public static readonly EnrollmentNotificationStatus Started = new EnrollmentNotificationStatus("STARTED", 1);
    public static readonly EnrollmentNotificationStatus Skipped = new EnrollmentNotificationStatus("SKIPPED", 2);
    public static readonly EnrollmentNotificationStatus Sending = new EnrollmentNotificationStatus("SENDING", 3);
    public static readonly EnrollmentNotificationStatus Failed = new EnrollmentNotificationStatus("FAILED", 4);
    public static readonly EnrollmentNotificationStatus Completed = new EnrollmentNotificationStatus("COMPLETED", 5);
    public static readonly EnrollmentNotificationStatus MarkedCompleted = new EnrollmentNotificationStatus("MARKCOMPLETED", 6);

    private EnrollmentNotificationStatus(string name, int value) : base(name, value)
    {
    }
}
