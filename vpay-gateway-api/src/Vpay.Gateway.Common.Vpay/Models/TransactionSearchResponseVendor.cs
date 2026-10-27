using System.Text.Json.Serialization;
using Newtonsoft.Json;
using VPay.Gateway.Common.Helpers;
using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;

namespace Vpay.Gateway.Common.Vpay.Models;

public class TransactionSearchResponseVendor
{
    public long vpayTransactionID { get; set; }
    public string? PreTransID { get; set; }

    public string? PaymentModality { get; set; }

    public DateTime? AchInitiatedDate { get; set; }
    public DateTime? AchSettlementDate { get; set; }
    public DateTime? CheckClearDate { get; set; }
    public DateTime? CheckCreatedDate { get; set; }

    public string? CheckNumber { get; set; }

    public string? BillingEntityCode { get; set; }

    public string? ClientCode { get; set; }
    public string? Bank { get; set; }

    [Newtonsoft.Json.JsonConverter(typeof(DecimalJsonConverter))]
    public decimal? Amount { get; set; }
    [Newtonsoft.Json.JsonConverter(typeof(DecimalJsonConverter))]
    public decimal? CurrentBalance { get; set; }
    [Newtonsoft.Json.JsonConverter(typeof(DecimalJsonConverter))]
    public decimal? AvailableBalance { get; set; }

    public string? GroupNumber { get; set; }

    public ICollection<TransactionEvent> ActionList { get; set; }

    [JsonIgnore]
    public List<ReissueTransactionIDs>? TransactionChainResponseInternal { get; set; } = new List<ReissueTransactionIDs>();

    [JsonProperty(Order = int.MaxValue)]
    [JsonPropertyName("correspondence")]
    public Correspondence? Correspondence { get; set; } = new Correspondence();
    // Custom property to return the desired response format
    [JsonProperty(Order = int.MaxValue)]
    [JsonPropertyName("ReissueTransactionIDs")]
    public List<string> ReissueTransactionIDs => TransactionChainResponseInternal?
        .Select(t => t.TransactionId ?? string.Empty)
        .ToList() ?? new List<string>();

}
