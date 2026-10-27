using VPay.Gateway.Common.Http;

namespace VPay.Gateway.Common.Enrollments;

/// <summary>
/// Configuration related specifically to the Enrollments API.
/// </summary>
public class EnrollmentApiHttpClientConfig : HttpClientConfig
{
    /// <summary>
    /// Gets or sets the access token for the Enrollment API.
    /// </summary>
    public string ApiToken { get; set; } = "";
}
