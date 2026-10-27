using MediatR;
using Refit;

namespace VPay.Gateway.Common.PlatformConfig;

public class BillingEntityQuery : IRequest<BillingEntityDto?>
{
    public BillingEntityQuery(string clientCode, string billingEntityCode)
    {
        ClientCode = clientCode;
        BillingEntityCode = billingEntityCode;
    }

    public string ClientCode { get; }

    public string BillingEntityCode { get; }
}

public class BillingEntityQueryHandler : IRequestHandler<BillingEntityQuery, BillingEntityDto?>
{
    private readonly IPlatformConfigApi _platformConfigApi;

    public BillingEntityQueryHandler(IPlatformConfigApi platformConfigApi) =>
        _platformConfigApi = platformConfigApi ?? throw new ArgumentNullException(nameof(platformConfigApi));

    public async Task<BillingEntityDto?> Handle(BillingEntityQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var billingEntity = await _platformConfigApi.GetBillingEntity(request.ClientCode, request.BillingEntityCode, cancellationToken).ConfigureAwait(false);
            return billingEntity;
        }
        catch (ApiException apiEx) when (apiEx.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}
