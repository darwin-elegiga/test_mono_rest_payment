namespace VPay.Gateway.Common.Recon.Models;

public class ClientReconDto
{
    public string? OriginCode { get; set; }

    public string? ClientCode { get; set; }

    public List<ReconPaymentInstructionDto>? PaymentInstructions { get; set; }
}
