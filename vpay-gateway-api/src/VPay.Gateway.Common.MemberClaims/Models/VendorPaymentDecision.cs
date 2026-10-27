using System.Text.Json.Serialization;

namespace VPay.Gateway.Common.MemberClaims.Models;

/// <summary>
/// Represents a request for vendor payment decision.
/// </summary>
public class VendorPaymentDecision
{
    /// <summary>
    /// The Client Code. Required.
    /// </summary>
    [JsonPropertyName("payerCode")]
    public string PayerCode { get; set; } = string.Empty;

    /// <summary>
    /// The Billing Entity Code. Required.
    /// </summary>
    [JsonPropertyName("subpayerCode")]
    public string SubpayerCode { get; set; } = string.Empty;

    /// <summary>
    /// The vendor unique Pre-Transaction Id. Required.
    /// </summary>
    [JsonPropertyName("preTransId")]
    public string PreTransId { get; set; } = string.Empty;

    /// <summary>
    /// The list of members for the payment. Required, at least one member must be provided,
    /// but no more than one member can be provided for (single-member payments).
    /// </summary>
    [JsonPropertyName("memberIDs")]
    public IList<VendorPaymentDecisionMember> MemberIDs { get; set; } = Array.Empty<VendorPaymentDecisionMember>();

    /// <summary>
    /// The payment type selected for the payment (Check, ACH/EFT, or Digital Card) if the claims were accepted,
    /// or Rejection if payment claims were not accepted. Required.
    /// </summary>
    [JsonPropertyName("decisionedModality")]
    public string DecisionedModality { get; set; } = string.Empty;

    /// <summary>
    /// The flag to determine if a selection has expired or not. Required.
    /// </summary>
    [JsonPropertyName("noSelectionExpiredIndicator")]
    public bool NoSelectionExpiredIndicator { get; set; }

    /// <summary>
    /// The reason for the rejection of the payment claims. Optional, required if Decision Modality is 'PREISREJ'
    /// </summary>
    [JsonPropertyName("claimRejectionReason")]
    public string ClaimRejectionReason { get; set; } = string.Empty;

    /// <summary>
    /// The vendor unique Transaction Id. Required.
    /// </summary>
    [JsonPropertyName("zetaTransactionId")]
    public string ZetaTransactionId { get; set; } = string.Empty;
}
