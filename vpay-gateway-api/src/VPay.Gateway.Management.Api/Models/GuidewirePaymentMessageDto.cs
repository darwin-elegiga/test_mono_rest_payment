namespace VPay.Gateway.Management.Api.Models;

public class GuidewirePaymentMessageDto
{
    public string ClientCode { get; set; }

    public string PaymentId { get; set; }

    public SendGuidewirePaymentStatus? Status { get; set; }

    public string Note { get; set; }
}
