namespace VPay.Gateway.Common.Kubera.Models;

public class Transaction
{
    public long TransactionId { get; set; }

    public string ClientCode { get; set; } = "";

    public string BillingEntityCode { get; set; } = "";

    public string PaymentTypeCode { get; set; } = "";

    public string ClaimId { get; set; } = "";

    public string ClientReferenceId { get; set; } = "";

    public string GroupNumber { get; set; } = "";

    public string ClientGroup { get; set; } = "";

    public string PaymentId { get; set; } = "";

    public string PayeeName { get; set; } = "";

    public string PayeeId { get; set; } = "";

    public string TaxId { get; set; } = "";

    public string Requestor { get; set; } = "";

    public DateTime DateLoaded { get; set; }

    public decimal Amount { get; set; }

    public decimal CurrentBalance { get; set; }

    /// <summary>
    /// Is the original record that was loaded from the file sent to us.
    /// </summary>
    public bool IsOriginalLoad { get; set; }

    public List<TransactionEvent>? Events { get; set; }
}
