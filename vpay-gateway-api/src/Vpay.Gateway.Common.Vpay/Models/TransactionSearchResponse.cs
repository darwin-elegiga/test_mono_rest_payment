using System.Text.Json;
using Newtonsoft.Json;
using VPay.Gateway.Common.Helpers;

namespace Vpay.Gateway.Common.Vpay.Models;

public class TransactionSearchResponse
{
    public string? TransactionType { get; set; } = string.Empty;

    [JsonProperty("loadDate")]
    public DateTime? LoadDate
    {
        get; set;
    }

    [JsonProperty("billingEntity")]
    public BillingEntity? BillingEntity { get; set; }

    [JsonProperty("billingEntityCode")]
    public string? BillingEntityCode { get; set; }

    [JsonProperty("groupNumber")]
    public string? GroupNumber { get; set; }


    [JsonProperty("clientGroup")]
    [JsonIgnore]
    public string? ClientGroup { get; set; }

    [JsonProperty("bankName")]
    public string? BankName { get; set; }

    [JsonProperty("lastUpdated")]
    public DateTime? LastUpdated { get; set; }

    [JsonProperty("amount")]
    [JsonConverter(typeof(DecimalJsonConverter))]
    public decimal? Amount { get; set; }

    [JsonProperty("availableBalance")]
    [JsonConverter(typeof(DecimalJsonConverter))]
    public decimal? AvailableBalance { get; set; }

    [JsonProperty("currentBalance")]
    [JsonConverter(typeof(DecimalJsonConverter))]
    public decimal? CurrentBalance { get; set; }

    [JsonProperty("statusMessage")]
    public string? StatusMessage { get; set; }

    [JsonProperty("posPayCheckNumber")]
    public string? PosPayCheckNumber { get; set; }

    [JsonProperty("events")]
    public ICollection<TransactionEvent> Events { get; set; } = new List<TransactionEvent>();
}
