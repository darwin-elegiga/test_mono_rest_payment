namespace VPay.Gateway.Api.Models;

public class VendorUnloadPaymentRequestDto
{
    /// <summary>
    /// TransactionId passed by Vendor
    /// </summary>
    public long TransactionId { get; set; }

    /// <summary>
    /// Requester name passed by Vendor
    /// </summary>
    public string RequesterName { get; set; } = string.Empty;

    /// <summary>
    /// Optional Reason Code passed by Vendor
    /// </summary>
    public string ReasonCode { get; set; } = string.Empty;
}
