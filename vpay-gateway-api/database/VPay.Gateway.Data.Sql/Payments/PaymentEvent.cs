namespace VPay.Gateway.Data.Sql.Payments;

public class PaymentEvent
{
    public long Id { get; set; }

    public long PaymentId { get; set; }

    public long TransactionId { get; set; }

    public long TransactionEventId { get; set; }

    public string EventStatus { get; set; } = "";

    public DateTime EventDateTime { get; set; }

    public string EventReasonCode { get; set; } = "";

    public string EventReasonDescription { get; set; } = "";

    public string EventPaymentType { get; set; } = "";

    public string EventTrackingNumber { get; set; } = "";

    public string CurrentStatus { get; set; } = "";

    public DateTime CreatedDateTime { get; set; }

    public DateTime LastModifiedDateTime { get; set; }
}
