namespace VPay.Gateway.Common.Enrollments.Models;

public class BatchExistingClaimsResponse
{
    /// <summary>
    /// The results of the API call. The Key is the Member ID for the enrollment, and
    /// the Value contains the enrollment details
    /// </summary>
    public Dictionary<string, IEnumerable<dynamic>> Results { get; } = new(StringComparer.OrdinalIgnoreCase);
}
