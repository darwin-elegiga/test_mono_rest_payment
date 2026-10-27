using Newtonsoft.Json;

namespace VPay.DocSys.Parsing.Http.Abstractions.PlatformConfiguration.Dto
{
    public class CustomSendBackClientBoardingConfigDto : ClientBoardingConfigDtoBase
    {
        [JsonProperty("data1")]
        [JsonConverter(typeof(PlatformConfigurationBoolConverter))]
        public bool? IsCustomSendBack { get; set; }
    }
}
