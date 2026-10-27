namespace VPay.Gateway.Common.Preferencing.Models;

public class NetworkProviderEnrollmentDto
{
    /// <summary>
    /// The unique identifier for the network enrollment.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// The unique identifier for the network.
    /// </summary>
    public long NetworkId { get; set; }

    /// <summary>
    /// The partner network name.
    /// </summary>
    public string NetworkPartner { get; set; } = string.Empty;

    /// <summary>
    /// The provider Tax Id.
    /// </summary>
    public string ProviderTin { get; set; } = string.Empty;

    /// <summary>
    /// The provider National Provider Id.
    /// </summary>
    public string ProviderNpi { get; set; } = string.Empty;

    /// <summary>
    /// The enrollment type: B2B or C2B.
    /// </summary>
    public string EnrollmentType { get; set; } = string.Empty;

    /// <summary>
    /// The enrollment start date, when it becomes active.
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// The enrollment end date, when it becomes inactive.
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// The user or program that created the original enrollment record.
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// The original creation time stamp of the enrollment record.
    /// </summary>
    public DateTime CreatedDateTime { get; set; }

    /// <summary>
    /// The user or program that modified the enrollment record most recently.
    /// </summary>
    public string LastModifiedBy { get; set; } = string.Empty;

    /// <summary>
    /// The most recent modification time stamp of the enrollment record.
    /// </summary>
    public DateTime LastModifiedDateTime { get; set; }
}
