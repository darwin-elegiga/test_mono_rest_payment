using Newtonsoft.Json;

namespace VPay.Optum.PaymentRouting.Application.Models.RabbitMessages
{
    public class BatchComplete
    {
        [JsonProperty("batchId")]
        public long InboundBatchId { get; set; }
        public string OptumBatchId { get; set; } = string.Empty;
    }
}
