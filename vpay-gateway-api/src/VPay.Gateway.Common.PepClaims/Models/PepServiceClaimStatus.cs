using Newtonsoft.Json;

namespace VPay.Gateway.Common.PepClaims.Models;

public class PepServiceClaimStatus
{
    public string ClaimId { get; set; } = "";

    [JsonProperty("origin")]
    public string OriginCode { get; set; } = "";

    [JsonProperty("tpaCode")]
    public string ClientCode { get; set; } = "";

    public string? BillingEntityId { get; set; }
    public decimal PaymentAmount { get; set; }
    public string? PayerId { get; set; }
    public string? PaymentCurrency { get; set; }
    public string? ClaimStatus { get; set; }
    public DateTime? ClaimCreated { get; set; }
    public string? ClaimNumber { get; set; }
    public string? BillingEntityName { get; set; }
    public string? ClientReferenceId { get; set; }
    public string? PaymentId { get; set; }
    public DateTime? PepTimeout { get; set; }
    public string? HeaderDescription { get; set; }
    public string? HeaderNote { get; set; }
    public DateTime? ClaimStatusCreated { get; set; }
}
