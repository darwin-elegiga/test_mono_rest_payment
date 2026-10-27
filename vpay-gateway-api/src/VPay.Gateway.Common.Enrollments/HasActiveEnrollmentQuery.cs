using MediatR;
using VPay.Gateway.Common.Enrollments.Models;

namespace VPay.Gateway.Common.Enrollments;

/// <summary>
/// Query used to determine whether or not an existing claim
/// </summary>
public sealed record HasActiveEnrollmentQuery(
    string ClientCode,
    string BillingEntityCode,
    string MemberId
) : IRequest<bool>;

public class HasActiveEnrollmentQueryHandler : IRequestHandler<HasActiveEnrollmentQuery, bool>
{
    private readonly IEnrollmentsApi _enrollmentsApi;

    public HasActiveEnrollmentQueryHandler(IEnrollmentsApi enrollmentsApi) =>
        _enrollmentsApi = enrollmentsApi ?? throw new ArgumentNullException(nameof(enrollmentsApi));

    public async Task<bool> Handle(HasActiveEnrollmentQuery request, CancellationToken cancellationToken)
    {
        var claimRequest = new BatchExistingClaimsRequest
        {
            ClientCode = request.ClientCode,
            BillingEntityCode = request.BillingEntityCode,
            MemberIds = new List<string> { request.MemberId }
        };

        var result = await _enrollmentsApi.GetActiveEnrollments(claimRequest, cancellationToken).ConfigureAwait(false);

        // if there are any items for the member id, there is an enrollment in progress
        return result.Results[request.MemberId].Any();
    }
}
