using VPay.Gateway.Common.Models;

namespace VPay.Gateway.Common.Commands;

public class SendGuidewireMessage
{
    public GatewayCallType CallType { get; set; }

    public string? ClientCode { get; set; }

    public string? ClaimNumber { get; set; }

    public string? PayeeId { get; set; }

    public string? PaymentId { get; set; }

    public string? Status { get; set; }

    public GuidewireNote? Note { get; set; }
}
