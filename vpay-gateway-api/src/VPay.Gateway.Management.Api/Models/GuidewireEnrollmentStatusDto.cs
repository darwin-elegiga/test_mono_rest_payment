namespace VPay.Gateway.Management.Api.Models;

public class GuidewireEnrollmentStatusDto
{
    public long PayeeStatusId { get; set; }

    public string ClientCode { get; set; }

    public string MemberId { get; set; }

    public long PayeeId { get; set; }

    public string ClaimId { get; set; }

    public string ProcessStatus { get; set; }

    public long PepPayeeId { get; set; }

    public long PepStatusId { get; set; }

    public long PartyStatusId { get; set; }

    public string PartyStatus { get; set; }

    public int RetryCount { get; set; }

    public DateTime StatusDateTime { get; set; }

    public DateTime CreatedDateTime { get; set; }

    public DateTime LastModifiedDateTime { get; set; }
}
