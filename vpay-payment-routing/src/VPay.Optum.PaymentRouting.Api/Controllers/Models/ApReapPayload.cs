using Newtonsoft.Json;
using VPay.Optum.PaymentRouting.Common.Serialization;

namespace VPay.Optum.PaymentRouting.Api.Controllers.Models
{
    public class ApReapPayload
    {
        [JsonProperty("payr835ID")]
        public string PayerID { get; set; }
        public string PaymentSource { get; set; }
        public string PaymentType { get; set; }
        [JsonProperty("payerSubpayerID")]
        public string SubpayerID { get; set; }
        public string ProviderNumber { get; set; }
        public string ProviderName { get; set; }
        [JsonProperty("provNPINumber")]
        public string ProviderNPINumber { get; set; }
        [JsonProperty("unConsolPayNumber")]
        public string UnconsolPaymentNumber { get; set; }
        [JsonProperty("unConsolPayAmount")]
        public string UnconsolPaymentAmount { get; set; }
        [JsonProperty("displConsolPayNumber")]
        public string DisplayConsolPaymentNumber { get; set; }
        public string ConsolAmount { get; set; }
        [JsonProperty("payMethodCode")]
        public string PaymentMethodCode { get; set; }
        public string SettlementDate { get; set; }
        public string CheckNumber { get; set; }
        public string ReturnReasonCode { get; set; }
        public string RevisedMethodCode { get; set; }
        public string ReOriginatedDate { get; set; }


        public VpayApReap ToAvro()
        {
            return new VpayApReap
            {
                payr835ID = PayerID,
                paymentSource = PaymentSource,
                paymentType = PaymentType,
                payerSubpayerID = SubpayerID,
                providerNumber = ProviderNumber,
                providerName = ProviderName,
                provNPINumber = ProviderNPINumber,
                unConsolPayNumber = UnconsolPaymentNumber,
                unConsolPayAmount = UnconsolPaymentAmount,
                displConsolPayNumber = DisplayConsolPaymentNumber,
                consolAmount = ConsolAmount,
                payMethodCode = PaymentMethodCode,
                settlementDate = SettlementDate,
                checkNumber = CheckNumber,
                returnReasonCode = ReturnReasonCode,
                revisedMethodCode = RevisedMethodCode,
                reoriginatedDate = ReOriginatedDate
            };
        }

        public static ApReapPayload Create(VpayApReap avro)
        {
            return new ApReapPayload
            {
                PayerID = avro.payr835ID,
                PaymentSource = avro.paymentSource,
                PaymentType = avro.paymentType,
                SubpayerID = avro.payerSubpayerID,
                ProviderNumber = avro.providerNumber,
                ProviderName = avro.providerName,
                ProviderNPINumber = avro.provNPINumber,
                UnconsolPaymentNumber = avro.unConsolPayNumber,
                UnconsolPaymentAmount = avro.unConsolPayAmount,
                DisplayConsolPaymentNumber = avro.displConsolPayNumber,
                ConsolAmount = avro.consolAmount,
                PaymentMethodCode = avro.payMethodCode,
                SettlementDate = avro.settlementDate,
                CheckNumber = avro.checkNumber,
                ReturnReasonCode = avro.returnReasonCode,
                RevisedMethodCode = avro.revisedMethodCode,
                ReOriginatedDate = avro.reoriginatedDate
            };
        }
    }
}
