using System.ComponentModel.DataAnnotations;

namespace VPay.Gateway.Management.Api.Models;

public class GuidewireEnrollmentStatusResetDto
{
    /// <summary>
    /// The client code of the payee status to reset.
    /// Required.
    /// </summary>
    [Required, MaxLength(10)]
    public string ClientCode { get; set; }

    /// <summary>
    /// The member id of the payee status to reset.
    /// Required.
    /// </summary>
    [Required, MaxLength(20)]
    public string MemberId { get; set; }

    /// <summary>
    /// The claim number of the payee status to reset.
    /// Required.
    /// </summary>
    [Required, MaxLength(32)]
    public string ClaimNumber { get; set; }
}
