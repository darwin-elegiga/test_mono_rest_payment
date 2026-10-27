using MediatR;
using Refit;

namespace VPay.Gateway.Common.PlatformConfig;

public class EnrollmentConfigurationQuery : IRequest<MemberPaymentEnrolledConfigDto?>
{
    public EnrollmentConfigurationQuery(string originCode, string clientCode)
    {
        OriginCode = originCode;
        ClientCode = clientCode;
    }

    public string OriginCode { get; }

    public string ClientCode { get; }
}

public class EnrollmentConfigurationQueryHandler : IRequestHandler<EnrollmentConfigurationQuery, MemberPaymentEnrolledConfigDto?>
{
    private readonly IPlatformConfigApi _platformConfigApi;

    public EnrollmentConfigurationQueryHandler(IPlatformConfigApi platformConfigApi) =>
        _platformConfigApi = platformConfigApi ?? throw new ArgumentNullException(nameof(platformConfigApi));

    public async Task<MemberPaymentEnrolledConfigDto?> Handle(EnrollmentConfigurationQuery request, CancellationToken cancellationToken)
    {
        try
        {
            return await _platformConfigApi.GetMemberPaymentEnrolled(request.OriginCode, request.ClientCode, cancellationToken).ConfigureAwait(false);
        }
        catch (ApiException apiEx) when (apiEx.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}
