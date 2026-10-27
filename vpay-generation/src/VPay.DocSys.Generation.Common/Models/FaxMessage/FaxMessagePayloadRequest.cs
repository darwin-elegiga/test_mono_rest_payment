using Newtonsoft.Json;

namespace VPay.DocSys.Generation.Common
{
    public class FaxMessagePayloadRequest : FaxMessagePayload
    {
        /// <summary>
        /// This is used for the tiff queue and fax fan out exchange
        /// </summary>
        [JsonProperty(propertyName: "pri")]
        public byte Priority { get; set; }
    }
}
