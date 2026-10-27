using Newtonsoft.Json;

namespace VPay.Optum.PaymentRouting.Common.Serialization
{
    public class PaymentRecord
    {
        /// <summary>
        /// The Optum Batch id that is passed on the messages
        /// </summary>
        public string? BatchId { get; set; }
        public string? FileProcId { get; set; }
        public string? PayerAlias { get; set; }
        public string? TransactionType { get; set; }

        [JsonProperty("provTIN")]
        public string? ProviderTin { get; set; }

        [JsonProperty("payer835ID")]
        public string? Payer835Id { get; set; }

        [JsonProperty("payerSubpayerID")]
        public string? PayerSubpayerId { get; set; }

        public string? ZipCode { get; set; }

        [JsonProperty("unconsolPaymentNbr")]
        public string? UnconsolidatedPaymentNbr { get; set; }

        [JsonProperty("uconslPaymentAmount")]
        public string? UnconsolidatedPaymentAmount { get; set; }

        [JsonProperty("blobStorageURI")]
        public string? BlobStorageUri { get; set; }

        public string? PaymentClob { get; set; }

        public string RemitMethod { get; set; } = default!;
    }
}
