namespace VPay.Gateway.Data.Sql.DocumentNotifications;

public class DocumentNotification
{
    public long Id { get; set; }

    public long TransactionId { get; set; }

    public long ArchiveDetailId { get; set; }

    public string ClientCode { get; set; } = default!;

    public string BillingEntityCode { get; set; } = default!;

    public string? BillingEntityType { get; set; }

    public string DocumentDescription { get; set; } = "";

    public string DocumentType { get; set; } = "";

    public string DocumentName { get; set; } = "";

    public string PaymentId { get; set; } = "";

    public string CurrentStatus { get; set; } = default!;

    public DateTime CreatedDateTime { get; set; }

    public DateTime LastModifiedDateTime { get; set; }
}
