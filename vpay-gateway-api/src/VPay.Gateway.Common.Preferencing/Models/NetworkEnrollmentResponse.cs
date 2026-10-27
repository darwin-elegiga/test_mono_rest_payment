namespace VPay.Gateway.Common.Preferencing.Models;

public class NetworkEnrollmentResponse
{
    /// <summary>
    /// The name of the network partner.
    /// </summary>
    public string NetworkPartner { get; set; } = string.Empty;

    /// <summary>
    /// A unique identifier to match request with response.
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// The total number of enrollments in the request.
    /// </summary>
    public int EnrollmentCount { get; set; }
}
