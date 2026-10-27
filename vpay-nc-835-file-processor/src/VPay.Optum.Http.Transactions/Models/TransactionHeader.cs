using System.Text.Json.Serialization;

namespace VPay.Optum.Http.Transactions.Models
{
    public class TransactionHeader
    {
        [JsonPropertyName("ACHFCHDRID")]
        public long AchHeaderId { get; set; }

        [JsonPropertyName("OPACHYN")]
        public string OpachYn { get; set; }

        [JsonPropertyName("TXNCOUNT")]
        public int Count { get; set; }

        [JsonPropertyName("LISTSTART")]
        public int PageStart { get; set; }

        [JsonPropertyName("LISTEND")]
        public int PageEnd { get; set; }
    }
}
