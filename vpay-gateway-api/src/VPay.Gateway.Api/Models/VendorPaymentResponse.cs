namespace VPay.Gateway.Api.Models;

/// <summary>
/// Provides custom responses as required by some of the vendor payment endpoints.
/// </summary>
public class VendorPaymentResponse
{
    public VendorPaymentResponseDto Ok(string message = null)
    {
        return new VendorPaymentResponseDto
        {
            Status = "success",
            Message = message ?? "The request has been received and processed successfully."
        };
    }
}
