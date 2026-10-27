using VPay.Gateway.Common.Guidewire.Models;
using VPay.Gateway.Common.PlatformConfig;

namespace VPay.Gateway.Management.Application;

public static class Helpers
{
    public static GuidewireSettings ToGuidewireSettings(this ExternalClientConfigurationDto config)
    {
        return new GuidewireSettings
        {
            Password = config.Password,
            UserName = config.Username,
            Url = config.Url
        };
    }
}
