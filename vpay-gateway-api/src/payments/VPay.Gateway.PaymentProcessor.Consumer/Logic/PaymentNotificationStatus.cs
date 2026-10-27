using Ardalis.SmartEnum;

namespace VPay.Gateway.PaymentProcessor.Consumer.Logic;

public class PaymentNotificationStatus : SmartEnum<PaymentNotificationStatus>
{
    public static readonly PaymentNotificationStatus Scheduled = new("SCHEDULED", 0);
    public static readonly PaymentNotificationStatus Started = new("STARTED", 1);
    public static readonly PaymentNotificationStatus Skipped = new("SKIPPED", 2);
    public static readonly PaymentNotificationStatus Sending = new("SENDING", 3);
    public static readonly PaymentNotificationStatus Failed = new("FAILED", 4);
    public static readonly PaymentNotificationStatus Completed = new("COMPLETED", 5);
    public static readonly PaymentNotificationStatus MarkedCompleted = new("MARKCOMPLETED", 6);

    private PaymentNotificationStatus(string name, int value) : base(name, value)
    {
    }
}
