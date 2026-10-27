namespace VPay.Gateway.Data.Sql.Payments;

public class PaymentStatusTracking
{
    public long Id { get; set; }

    public long PaymentId { get; set; }

    public string Status { get; set; } = "";

    public DateTime CreatedDateTime { get; set; }
}
