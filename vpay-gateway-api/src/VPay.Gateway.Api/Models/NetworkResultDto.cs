namespace VPay.Gateway.Api.Models;

public class NetworkResultDto
{
    /// <summary>
    /// The network partner name.
    /// </summary>
    /// <example>OPTUM</example>
    public string NetworkPartner { get; init; }

    /// <summary>
    /// The network configuration status: OK/Error.
    /// </summary>
    /// <example>OK</example>
    public string NetworkConfigStatus { get; set; }

    /// <summary>
    /// The network active status: Active/Inactive.
    /// </summary>
    /// <example>OK</example>
    public string NetworkActiveStatus { get; set; }
}
