using System;
using Newtonsoft.Json;
using VPay.Optum.PaymentRouting.Common.Helpers;
using VPay.Optum.PaymentRouting.Common.RabbitMessages;

namespace VPay.Optum.PaymentRouting.Api.Controllers.Models
{
    public class ApReapISeriesPayload
    {
        public string Origin { get; set; }

        public string TpaCode { get; set; }

        public string BillingEntity { get; set; }

        public string MessageType { get; set; }

        public decimal TransactionId { get; set; }

        public string ProviderTin { get; set; }

        public string ProviderName { get; set; }

        public string ProviderNPPID { get; set; }

        public string UserKey { get; set; }

        [JsonConverter(typeof(JsonDecimalConverter))]
        public decimal LoadAmount { get; set; }

        [JsonConverter(typeof(ISeriesDateTimeConverter)), JsonProperty("settlementdate")]
        public DateTime LoadDate { get; set; }

        public string PaymentType { get; set; } = string.Empty;

        public string CheckNumber { get; set; } = string.Empty;

        public string TrnSegment { get; set; }

        public string ReapCheckNumber { get; set; } = string.Empty;

        public decimal? ReapTransactionId { get; set; }

        public string ReapReasonCode { get; set; } = string.Empty;

        [JsonProperty("revisedmethodcode")]
        public string ReapPaymentType { get; set; } = string.Empty;

        [JsonConverter(typeof(ISeriesDateTimeConverter)), JsonProperty("reoriginateddate")]
        public DateTime? ReapLoadDate { get; set; }
    }

    public static class ApReapISeriesPayloadExtensions
    {
        public static ApReapMessage ToRabbit(this ApReapISeriesPayload message)
        {
            return new ApReapMessage
            {
                Origin = message.Origin,
                TpaCode = message.TpaCode,
                BillingEntity = message.BillingEntity,
                MessageType = message.MessageType,
                TransactionId = message.TransactionId,
                ProviderTin = message.ProviderTin,
                ProviderName = message.ProviderName,
                ProviderNPPID = message.ProviderNPPID,
                UserKey = message.UserKey,
                LoadAmount = message.LoadAmount,
                LoadDate = message.LoadDate,
                PaymentType = message.PaymentType,
                CheckNumber = message.CheckNumber,
                TrnSegment = message.TrnSegment,
                ReapCheckNumber = message.ReapCheckNumber,
                ReapTransactionId = message.ReapTransactionId,
                ReapReasonCode = message.ReapReasonCode,
                ReapPaymentType = message.ReapPaymentType,
                ReapLoadDate = message.ReapLoadDate
            };
        }
    }
}
