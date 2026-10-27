namespace VPay.Gateway.Common.RestPayments;
/// <summary>
/// Unload Vendor Payment Response Object
/// </summary>
/// <param name="ResponseCode">Response Code</param>
/// <param name="ResponseDesc">Response Description</param>
/// <param name="ReasonCode">Reason Code</param>
/// <param name="ReasonDesc">Reason Description</param>
/// <param name="SuccessCode">Success Code</param>
/// <param name="SuccessDesc">Success Description</param>
public record UnloadVendorPaymentResponse(string ResponseCode, string ResponseDesc, string ReasonCode, string ReasonDesc,
    string SuccessCode, string SuccessDesc);
