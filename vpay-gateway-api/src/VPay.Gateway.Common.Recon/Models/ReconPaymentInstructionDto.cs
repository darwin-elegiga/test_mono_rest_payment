namespace VPay.Gateway.Common.Recon.Models;

public class ReconPaymentInstructionDto
{
    public string? PaymentId { get; set; }

    public BillingEntityInfo? BillingEntity { get; set; }

    public string? PayerId { get; set; }

    public List<ReconPaymentTransactionDto>? Transactions { get; set; }

    public List<ReconPreTransactionDto>? PreTransactions { get; set; }

    public List<ReconPreTransactionPartyDto>? PreTransactionParties { get; set; }
}
