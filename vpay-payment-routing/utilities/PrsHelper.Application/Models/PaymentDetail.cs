// See https://aka.ms/new-console-template for more information
using System.Text.Json.Serialization;

internal class PaymentDetail
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string UserKey { get; set; } = default!;
    public string ProvTin { get; set; } = default!;
    public string PayerSubpayerId { get; set; } = default!;
    public string ZipCode { get; set; } = default!;
    public decimal Amount { get; set; } = default!;
}
