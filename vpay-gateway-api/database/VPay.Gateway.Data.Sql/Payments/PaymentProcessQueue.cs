namespace VPay.Gateway.Data.Sql.Payments;

public class PaymentProcessQueue
{
    public long Id { get; set; }

    public string ClientCode { get; set; } = "";

    public string ClientPaymentId { get; set; } = "";

    public DateTime CreatedDateTime { get; set; }

    public DateTime LastRequestedDateTime { get; set; }

    public DateTime CurrentDatabaseTime { get; set; }
}
