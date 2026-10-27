using System.Collections.Generic;
using Newtonsoft.Json;

namespace VPay.Optum.Http.ConfigApi.Models
{
    public class ClientBoardingConfigurationDto
    {
        [JsonProperty]
        public List<ConfigurationDto> Items { get; set; }
    }
}
