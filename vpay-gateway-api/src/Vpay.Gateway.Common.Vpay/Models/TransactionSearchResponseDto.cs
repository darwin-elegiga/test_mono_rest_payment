

using System.Text.Json.Serialization;

namespace Vpay.Gateway.Common.Vpay.Models;

public class TransactionSearchResponseDto : TransactionSearchResponse
{
    [JsonIgnore]
    public new string? TransactionType { get; set; } // Exclude TransactionType from serialization

    public string? PaymentModality { get; set; }

    public DateTime? AchInitiatedDate { get; set; }
    public DateTime? AchSettlementDate { get; set; }

    public DateTime? CheckCheckClearDate { get; set; }
    public DateTime? CheckCheckCreatedDate { get; set; }
}
