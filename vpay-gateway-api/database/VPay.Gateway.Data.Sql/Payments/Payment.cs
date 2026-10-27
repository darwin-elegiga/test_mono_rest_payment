namespace VPay.Gateway.Data.Sql.Payments;

public class Payment
{
    public long Id { get; set; }

    public string ClientCode { get; set; } = "";

    public string ClientPaymentId { get; set; } = "";

    public string LastSentStatus { get; set; } = "";

    public decimal CurrentBalance { get; set; }

    public DateTime CreatedDateTime { get; set; }

    public DateTime LastModifiedDateTime { get; set; }
}
