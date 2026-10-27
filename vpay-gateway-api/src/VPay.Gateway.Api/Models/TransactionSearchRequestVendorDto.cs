namespace VPay.Gateway.Api.Models;

public class TransactionSearchRequestVendorDto
{
    public string PayerCode { get; set; } = string.Empty;

    /// <summary>
    /// Sub Payer code to be passed by Vendor
    /// </summary>
    public string SubPayerCode { get; set; } = string.Empty;

    /// <summary>
    /// PreTransactionId passed by Vendor
    /// </summary>
    public string PreTransactionId { get; set; } = string.Empty;

    /// <summary>
    /// VpayTransactionId passed by Vendor
    /// </summary>
    public long TransactionId { get; set; }
}
