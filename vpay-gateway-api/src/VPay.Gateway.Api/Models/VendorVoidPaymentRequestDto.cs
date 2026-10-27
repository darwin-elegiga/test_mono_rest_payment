namespace VPay.Gateway.Api.Models;

public class VendorVoidPaymentRequestDto
{
    /// <summary>
    /// TransactionId passed by Vendor
    /// </summary>
    public long TransactionId { get; set; }

    /// <summary>
    /// Requester name passed by Vendor
    /// </summary>
    public string RequesterName { get; set; } = string.Empty;
}
