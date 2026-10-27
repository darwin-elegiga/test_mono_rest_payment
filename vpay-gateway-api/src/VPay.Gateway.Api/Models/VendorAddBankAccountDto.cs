using System.ComponentModel.DataAnnotations;

namespace VPay.Gateway.Api.Models;

public class VendorAddBankAccountDto
{
    [Required(AllowEmptyStrings = false, ErrorMessage = "PayerCode is required.")]
    public string PayerCode { get; set; }

    [Required(AllowEmptyStrings = false, ErrorMessage = "SubPayerCode is required.")]
    public string SubPayerCode { get; set; }

    [Required(AllowEmptyStrings = false, ErrorMessage = "ClientMemberId is required.")]
    public string ClientMemberId { get; set; }

    [Required(AllowEmptyStrings = false, ErrorMessage = "ZetaMemberId is required.")]
    public string ZetaMemberId { get; set; }

    [Required(AllowEmptyStrings = false, ErrorMessage = "BankRoutingNumber is required.")]
    [StringLength(9, ErrorMessage = "The bank routing number must not exceed 9 characters")]
    public string BankRoutingNumber { get; set; }

    [Required(AllowEmptyStrings = false, ErrorMessage = "BankAccountNumber is required.")]
    [StringLength(32, ErrorMessage = "The bank account number must not exceed 32 characters.")]
    public string BankAccountNumber { get; set; }
}
