using Newtonsoft.Json;

namespace VPay.Gateway.Common.PlatformConfig;

public class ClientBoardingConfigurationDto
{
    [JsonProperty]
    public List<ClientConfigurationDto> Items { get; set; } = new List<ClientConfigurationDto>();
}
