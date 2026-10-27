using Newtonsoft.Json;

namespace VPay.Gateway.Common.Enrollments.Models;

public class EnrollmentRequest
{
    [JsonProperty(PropertyName = "payee_list")]
    public List<PartyEnrollmentRequest> PayeeList { get; } = new List<PartyEnrollmentRequest>();
}
