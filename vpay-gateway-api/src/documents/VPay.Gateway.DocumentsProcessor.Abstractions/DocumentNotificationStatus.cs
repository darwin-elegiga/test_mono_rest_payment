using Ardalis.SmartEnum;

namespace VPay.Gateway.DocumentsProcessor.Abstractions;

public sealed class DocumentNotificationStatus : SmartEnum<DocumentNotificationStatus>
{
    public static readonly DocumentNotificationStatus Scheduled = new("SCHEDULED", 0);
    public static readonly DocumentNotificationStatus Started = new("STARTED", 1);
    public static readonly DocumentNotificationStatus Skipped = new("SKIPPED", 2);
    public static readonly DocumentNotificationStatus Failed = new("FAILED", 3);
    public static readonly DocumentNotificationStatus Completed = new("COMPLETED", 4);
    public static readonly DocumentNotificationStatus MarkedCompleted = new("MARKCOMPLETED", 5);

    private DocumentNotificationStatus(string name, int value) : base(name, value)
    {
    }
}
