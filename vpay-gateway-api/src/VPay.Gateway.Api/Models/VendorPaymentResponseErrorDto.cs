namespace VPay.Gateway.Api.Models;

/// <summary>
/// Represents the error response returned when processing a vendor payment request fails.
/// </summary>
public class VendorPaymentResponseErrorDto
{
    public string Status { get; set; }
    public string ErrorCode { get; set; }
    public string Message { get; set; }
}
