namespace VPay.Gateway.Api.Models;

public class AddBankAccountFailureResponse
{
    public string Status { get; set; } = "failure";
    public string ErrorCode { get; set; }
    public string Message { get; set; }
}
