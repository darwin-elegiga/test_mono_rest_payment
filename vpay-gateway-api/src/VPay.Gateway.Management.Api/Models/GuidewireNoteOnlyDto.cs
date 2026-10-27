namespace VPay.Gateway.Management.Api.Models;

public class GuidewireNoteOnlyDto : GuidewireNoteDto
{
    public string ClientCode { get; set; }

    public string ClaimNumber { get; set; }

    public string PayeeId { get; set; }

    public string PaymentId { get; set; }
}
