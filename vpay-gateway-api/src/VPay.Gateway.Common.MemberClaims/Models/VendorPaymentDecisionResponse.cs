namespace VPay.Gateway.Common.MemberClaims.Models;

/// <summary>
/// Represents the response returned after processing a vendor payment request, including the status
/// and an associated message.
/// </summary>
/// <remarks>This data transfer object is typically used to convey the outcome of a vendor payment
/// request operation, such as approval or rejection, along with a descriptive message.</remarks>
public class VendorPaymentDecisionResponse
{
    public int? Status { get; set; } = 0;

    public string? Message { get; set; } = string.Empty;

    public string? TraceId { get; set; } = string.Empty;

    public object? Type { get; set; } = string.Empty;

    public string? Title { get; set; } = string.Empty;

    public string? Detail { get; set; } = string.Empty;

    public object? Instance { get; set; } = string.Empty;

    public IDictionary<string, string[]>? Errors { get; set; } = new Dictionary<string, string[]>();

    public override string ToString()
    {
        return Newtonsoft.Json.JsonConvert.SerializeObject(this);
    }
}
