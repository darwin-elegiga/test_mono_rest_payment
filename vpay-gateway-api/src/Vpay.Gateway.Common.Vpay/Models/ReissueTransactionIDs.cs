using Newtonsoft.Json;

namespace Vpay.Gateway.Common.Vpay.Models;

public class ReissueTransactionIDs
{
    [JsonProperty("transactionId")]
    public string? TransactionId { get; set; }
}
