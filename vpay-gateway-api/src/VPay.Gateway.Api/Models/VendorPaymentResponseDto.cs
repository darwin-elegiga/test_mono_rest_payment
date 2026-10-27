namespace VPay.Gateway.Api.Models;

/// <summary>
/// Represents the response returned after processing a vendor payment request, including the status
/// and an associated message.
/// </summary>
/// <remarks>This data transfer object is typically used to convey the outcome of a vendor payment
/// request operation, such as approval or rejection, along with a descriptive message.</remarks>
public class VendorPaymentResponseDto
{
    public string Status { get; set; }
    public string Message { get; set; }
}
