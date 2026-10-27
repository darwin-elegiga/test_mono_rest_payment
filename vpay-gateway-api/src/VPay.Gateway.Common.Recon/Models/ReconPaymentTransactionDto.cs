using Newtonsoft.Json;

namespace VPay.Gateway.Common.Recon.Models;

public class ReconPaymentTransactionDto
{
    public string? TransactionId { get; set; }

    public string? PaymentTypeCode { get; set; }

    [JsonConverter(typeof(UTCDateTimeConverter))]
    public DateTime? RequestedTime { get; set; }

    [JsonConverter(typeof(UTCDateTimeConverter))]
    public DateTime? LoadedOrDeniedTime { get; set; }

    public double? OriginalRequestedAmount { get; set; }

    public double? RequestedAmount { get; set; }

    public string? Currency { get; set; }

    public string? PositivePayCheckNumber { get; set; }

    public double? CurrentBalance { get; set; }

    public List<string>? ClaimIds { get; set; }

    public string? GroupNumber { get; set; }

    public string? ClaimData { get; set; }

    public string? PaymentData { get; set; }

    public Edi835BankAccountInfo? BankRoutingInfo835 { get; set; }

    public string? ClientReferenceId { get; set; }

    public BankAccountInfoDto? GuardianBankInfo { get; set; }

    public List<ReconPaymentTransactionStatusDto>? Statuses { get; set; }
}
