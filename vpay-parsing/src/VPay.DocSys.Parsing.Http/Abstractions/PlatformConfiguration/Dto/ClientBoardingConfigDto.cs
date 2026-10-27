using System;
using System.Linq;
using System.Linq.Expressions;
using Newtonsoft.Json;

namespace VPay.DocSys.Parsing.Http.Abstractions.PlatformConfiguration.Dto
{
    public abstract class ClientBoardingConfigDtoBase : IComparable<ClientBoardingConfigDtoBase>
    {
        public const string ACTIVE = "ACTIVE";
        public const string DEFAULT = "DEFAULT";

        [JsonProperty("originCode")]
        public string OriginCode { get; set; }

        [JsonProperty("clientCode")]
        public string ClientCode { get; set; }

        [JsonProperty("billingEntityCode")]
        public string BillingEntityCode { get; set; }

        [JsonProperty("paymentTypeCode")]
        public string PaymentTypeCode { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("beginDate")]
        public System.DateTime BeginDate { get; set; }

        [JsonProperty("endDate")]
        public System.DateTime EndDate { get; set; }

        [JsonProperty("createdDateTime")]
        public System.DateTime CreatedDateTime { get; set; }

        [JsonProperty("createdByProgram")]
        public string CreatedByProgram { get; set; }

        [JsonProperty("createdBy")]
        public string CreatedBy { get; set; }

        [JsonProperty("lastModifiedDateTime")]
        public System.DateTime LastModifiedDateTime { get; set; }

        [JsonProperty("lastModifiedByProgram")]
        public string LastModifiedByProgram { get; set; }

        [JsonProperty("lastModifiedBy")]
        public string LastModifiedBy { get; set; }

        #region Comparison
        public int CompareTo(ClientBoardingConfigDtoBase other)
        {
            return this.RelevanceByDefaults().CompareTo(other.RelevanceByDefaults());
        }

        public bool IsActive() => IsActive(DateTime.Now);
        public bool IsActive(DateTime dateTime) => Status == ACTIVE && BeginDate < dateTime && EndDate > dateTime;
        public bool IsDefault() => OriginCode == DEFAULT;
        /// <summary>
        /// lower number is less relevant (more defaults), higher number is more relevant (less defaults)
        /// </summary>
        /// <returns></returns>
        public int RelevanceByDefaults()
        {
            // put default item last
            var o = this.IsDefault() ? 0b0000 : 0b1000;
            // then empty client codes
            var t = string.IsNullOrEmpty(this.ClientCode) ? 0b0000 : 0b0100;
            // then empty billing entity codes
            var b = string.IsNullOrEmpty(this.BillingEntityCode) ? 0b0000 : 0b0010;
            // then empty payment codes
            var p = string.IsNullOrEmpty(this.PaymentTypeCode) ? 0b0000 : 0b0001;

            return o + t + b + p;
        }
        /// <summary>
        /// Is <paramref name="field"/> equal to any of the <paramref name="values"/>
        /// </summary>
        /// <param name="isEqual"></param>
        /// <param name="field"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        private static bool IsEqualAny(Func<string, string, bool> isEqual, string field, params string[] values)
            => values.Any(value => isEqual(field, value));
        /// <summary>
        /// Are <paramref name="field"/> and <paramref name="value"/> equal or are they equal in uppercase
        /// </summary>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static bool IsUpperEqual(string field, string value)
            => field?.ToUpper() == value?.ToUpper();
        /// <summary>
        /// negative numbers mean they are irrelevant (mismatched), lower positive numbers are less relevant (more defaults), higher numbers are mor relevant (less defaults)
        /// </summary>
        /// <param name="dateTime"></param>
        /// <param name="originCode"></param>
        /// <param name="clientCode"></param>
        /// <param name="billingEntityCode"></param>
        /// <param name="paymentTypeCode"></param>
        /// <returns></returns>
        public int Relevance(DateTime? dateTime = null, string originCode = null, string clientCode = null, string billingEntityCode = null, string paymentTypeCode = null)
        {
            if (!IsActive(dateTime ?? DateTime.Now))
            {
                return -1;
            }

            if (!string.IsNullOrEmpty(originCode) && !IsEqualAny(IsUpperEqual, OriginCode, originCode, DEFAULT))
            {
                return -1;
            }

            if ((!string.IsNullOrEmpty(clientCode) && !IsEqualAny(IsUpperEqual, ClientCode, clientCode, string.Empty, null))
                || (string.IsNullOrEmpty(clientCode) && !string.IsNullOrEmpty(ClientCode)))
            {
                return -1;
            }

            if ((!string.IsNullOrEmpty(billingEntityCode) && !IsEqualAny(IsUpperEqual, BillingEntityCode, billingEntityCode, string.Empty, null))
                || (string.IsNullOrEmpty(billingEntityCode) && !string.IsNullOrEmpty(BillingEntityCode)))
            {
                return -1;
            }

            if ((!string.IsNullOrEmpty(paymentTypeCode) && !IsEqualAny(IsUpperEqual, PaymentTypeCode, paymentTypeCode, string.Empty, null))
                || (string.IsNullOrEmpty(paymentTypeCode) && !string.IsNullOrEmpty(PaymentTypeCode)))
            {
                return -1;
            }

            return RelevanceByDefaults();
        }
        #endregion
    }

    public class ClientBoardingConfigDto : ClientBoardingConfigDtoBase
    {
        [JsonProperty("data1")]
        public virtual string Data1 { get; set; }

        [JsonProperty("data2")]
        public virtual string Data2 { get; set; }

        [JsonProperty("data3")]
        public virtual string Data3 { get; set; }

        [JsonProperty("data4")]
        public virtual string Data4 { get; set; }

        [JsonProperty("data5")]
        public virtual string Data5 { get; set; }

        [JsonProperty("data6")]
        public virtual string Data6 { get; set; }
    }
}
