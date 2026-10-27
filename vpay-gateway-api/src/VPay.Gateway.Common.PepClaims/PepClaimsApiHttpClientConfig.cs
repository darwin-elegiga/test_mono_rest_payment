using VPay.Gateway.Common.Http;

namespace VPay.Gateway.Common.PepClaims;

/// <summary>
/// Configuration related specifically to the PEP Claims API.
/// </summary>
public class PepClaimsApiHttpClientConfig : HttpClientConfig
{
    public PepClaimsApiHttpClientConfig()
    {
        Timeout = TimeSpan.FromSeconds(100);
    }
}
