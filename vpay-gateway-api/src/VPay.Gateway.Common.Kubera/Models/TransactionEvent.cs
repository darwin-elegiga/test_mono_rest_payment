namespace VPay.Gateway.Common.Kubera.Models;

public class TransactionEvent
{
    public long Id { get; set; }

    public long TransactionId { get; set; }

    public string Status { get; set; } = "";

    public DateTime TransactionDate { get; set; }

    public DateTime ProcessedDate { get; set; }

    public string BatchNumber { get; set; } = "";

    public decimal? Amount { get; set; }

    public string Merchant { get; set; } = "";

    public string RequesterName { get; set; } = "";

    public string ReasonCode { get; set; } = "";

    public string ReasonDescription { get; set; } = "";

    public string? TrackingNumber { get; set; }
}
