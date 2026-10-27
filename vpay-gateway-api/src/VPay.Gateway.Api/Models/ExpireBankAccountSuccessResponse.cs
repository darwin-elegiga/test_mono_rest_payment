using System.Text.Json.Serialization;

namespace VPay.Gateway.Api.Models;

public class ExpireBankAccountSuccessResponse
{
    public string Status { get; set; } = "success";

    [JsonPropertyName("optumMemberId")]
    public string OptumMemberId { get; set; }

}
