namespace VPay.Gateway.Management.Api.Models;

public class GuidewireEnrollmentStatusChangeDto
{
    public string ClientCode { get; set; }

    public string PayeeId { get; set; }

    public string ClaimNumber { get; set; }

    public string Status { get; set; }

    public GuidewireNoteDto Note { get; set; }
}
