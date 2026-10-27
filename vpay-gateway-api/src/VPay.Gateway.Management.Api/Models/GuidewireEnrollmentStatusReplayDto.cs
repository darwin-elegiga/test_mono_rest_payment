using System.ComponentModel;

namespace VPay.Gateway.Management.Api.Models;

public class GuidewireEnrollmentStatusReplayDto
{
    /// <summary>
    /// The optional client code filter, or null for enrollments from any client.
    /// The default is null.
    /// </summary>
    [DefaultValue(null)]
    public string ClientCode { get; set; } = null;

    /// <summary>
    /// The optional member id filter, or null for enrollments from any member.
    /// The default is null.
    /// </summary>
    [DefaultValue(null)]
    public string MemberId { get; set; } = null;

    /// <summary>
    /// The optional claim number filter, or null for enrollments from any claim.
    /// The default is null.
    /// </summary>
    [DefaultValue(null)]
    public string ClaimNumber { get; set; } = null;
}
