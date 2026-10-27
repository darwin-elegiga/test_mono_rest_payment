using Newtonsoft.Json;

namespace Vpay.Gateway.Common.Vpay.Models;
public class PaymentDetail
{
    public List<string> ClaimNumbers { get; set; } = new List<string>();
    public List<string> CheckNumbers { get; set; } = new List<string>();
    public List<string> FaxNumbers { get; set; } = new List<string>();
    public List<PaymentDetailEvent>? Events { get; set; }
    public string? FaxStatus { get; set; }
    public bool OriginalLoad { get; set; }
    public string? TaxId { get; set; }
    public string? ClientReferenceId { get; set; }
    public string? GroupNumber { get; set; }
    public string? ClientGroup { get; set; }
    [JsonProperty(PropertyName = "requestor")]
    public string? Requester { get; set; }
    public DateTime KuberaCreatedUtc { get; set; }
    public List<string>? PayeeNames { get; set; }
    public DateTime DateLoaded { get; set; }
    public string? Status { get; set; }
    public string? PaymentId { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal Amount { get; set; }
    public string? BillingEntityName { get; set; }
    public string? BillingEntity { get; set; }
    public string? BillingType { get; set; }
    public string? BillingTypeCode { get; set; }
    public string? Tpa { get; set; }
    public long TransactionId { get; set; }
    public string? PayeeId { get; set; }
    public DateTime KuberaUpdatedUtc { get; set; }
}
