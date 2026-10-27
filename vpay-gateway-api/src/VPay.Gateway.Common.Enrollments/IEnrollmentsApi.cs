using Refit;
using VPay.Gateway.Common.Enrollments.Models;

namespace VPay.Gateway.Common.Enrollments;

/// <summary>
/// Interface defining the shape of the Enrollments API.
/// </summary>
public interface IEnrollmentsApi
{
    [Post("/api/enrollment/start/enrollment")]
    Task CreateEnrollment([Body] EnrollmentRequest enrollmentRequest, CancellationToken token);

    [Post("/api/enrollment/existing/batch")]
    Task<BatchExistingClaimsResponse> GetActiveEnrollments([Body] BatchExistingClaimsRequest request, CancellationToken token);
}
