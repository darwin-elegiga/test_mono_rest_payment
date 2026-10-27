using Newtonsoft.Json;

namespace VPay.Gateway.Common.Enrollments.Models;

public class BatchExistingClaimsRequest
{
    public IReadOnlyCollection<string> MemberIds { get; init; } = default!;

    [JsonProperty(PropertyName = "tpa")]
    public string ClientCode { get; init; } = default!;

    [JsonProperty(PropertyName = "beCode")]
    public string BillingEntityCode { get; init; } = default!;
}
