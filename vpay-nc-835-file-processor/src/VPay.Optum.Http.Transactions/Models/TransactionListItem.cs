using System;
using System.Text.Json.Serialization;

namespace VPay.Optum.Http.Transactions.Models
{
    public class TransactionListItem
    {
        [JsonPropertyName("SETXID")]
        public long TransactionId { get; set; }

        [JsonPropertyName("PAYEENAME")]
        public string PayeeName { get; set; }

        [JsonPropertyName("PAYADD1")]
        public string PayeeAddress1 { get; set; }

        [JsonPropertyName("PAYADD2")]
        public string PayeeAddress2 { get; set; }

        [JsonPropertyName("PAYADD3")]
        public string PayeeAddress3 { get; set; }

        [JsonPropertyName("PAYCOCITY")]
        public string PayeeCity { get; set; }

        [JsonPropertyName("PAYCOSTATE")]
        public string PayeeState { get; set; }

        [JsonPropertyName("PAYPC")]
        public string PayeeZipCode { get; set; }

        [JsonPropertyName("LOADTS")]
        [JsonConverter(typeof(ISeriesDateTimeConverter))]
        public DateTime LoadedDateTime { get; set; }

        [JsonPropertyName("LOADAMOUNT")]
        public decimal LoadAmount { get; set; }

        [JsonPropertyName("PTAXID")]
        public string TaxId { get; set; }

        [JsonPropertyName("ORGNAME")]
        public string OriginCode { get; set; }

        [JsonPropertyName("TPA")]
        public string ClientCode { get; set; }

        [JsonPropertyName("BILENT")]
        public string BillingEntityCode { get; set; }

        [JsonPropertyName("VPPMTTYPE")]
        public string PaymentTypeCode { get; set; }
    }
}