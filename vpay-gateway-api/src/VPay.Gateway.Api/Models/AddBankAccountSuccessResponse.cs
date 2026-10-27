using System.Text.Json.Serialization;

namespace VPay.Gateway.Api.Models;

public class AddBankAccountSuccessResponse
{
    public string Status { get; set; } = "success";

    [JsonPropertyName("vPayMemberId")]
    public  string OptumMemberId { get; set; }
    
}
