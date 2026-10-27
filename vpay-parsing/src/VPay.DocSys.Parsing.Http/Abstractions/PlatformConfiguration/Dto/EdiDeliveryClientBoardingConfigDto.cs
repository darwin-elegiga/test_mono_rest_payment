using Newtonsoft.Json;

namespace VPay.DocSys.Parsing.Http.Abstractions.PlatformConfiguration.Dto
{
    public class EdiDeliveryClientBoardingConfigDto : ClientBoardingConfigDtoBase
    {
        public bool IsEnabled(EdiDeliveryDestination destination, EdiDeliveryFileExtension extension) => Destination == destination && FileExtension == extension;
        /// <summary>
        /// i.e. "Availity"
        /// </summary>
        [JsonProperty("data1")]
        [JsonConverter(typeof(EdiDeliveryDestinationEnumConverter))]
        public EdiDeliveryDestination Destination { get; set; }
        /// <summary>
        /// i.e. ".brn" or ".835"
        /// </summary>
        [JsonProperty("data2")]
        [JsonConverter(typeof(EdiDeliveryFileExtensionEnumConverter))]
        public EdiDeliveryFileExtension FileExtension { get; set; }
    }

    public enum EdiDeliveryDestination
    {
        None,
        Availity,
    }

    public enum EdiDeliveryFileExtension
    {
        None,
        Edi835,
        EdiBRN,
    }
}
