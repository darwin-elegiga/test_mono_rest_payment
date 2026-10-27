using System.ComponentModel.DataAnnotations;

namespace VPay.Gateway.Api.Models;

public class VendorExpireBankAccountDto
{
    [Required(AllowEmptyStrings = false, ErrorMessage = "PayerCode is required.")]
    public string PayerCode { get; set; }

    [Required(AllowEmptyStrings = false, ErrorMessage = "SubPayerCode is required.")]
    public string SubPayerCode { get; set; }

    [Required(AllowEmptyStrings = false, ErrorMessage = "ClientMemberId is required.")]
    public string ClientMemberId { get; set; }

    [Required(AllowEmptyStrings = false, ErrorMessage = "ZetaMemberId is required.")]
    public string ZetaMemberId { get; set; }
}
