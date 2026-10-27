using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace VPay.Optum.Http.Transactions.Models
{
    public class TransactionInfo
    {
        [JsonPropertyName("DETAILS")]
        public TransactionHeader Header { get; set; }

        [JsonPropertyName("NFPYDTLST")]
        public List<TransactionListItem> Items { get; set; }
    }
}