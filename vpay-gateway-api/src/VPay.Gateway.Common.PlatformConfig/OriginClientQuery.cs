using MediatR;
using Microsoft.Extensions.Logging;
using Refit;

namespace VPay.Gateway.Common.PlatformConfig;

public class OriginClientQuery : IRequest<OriginClientDto?>
{
    public OriginClientQuery(string clientCode) => ClientCode = clientCode;

    public string ClientCode { get; }
}

public class OriginClientQueryHandler : IRequestHandler<OriginClientQuery, OriginClientDto?>
{
    private readonly IPlatformConfigApi _platformConfigApi;

    public OriginClientQueryHandler(IPlatformConfigApi platformConfigApi)
    {
        _platformConfigApi = platformConfigApi;
    }

    public async Task<OriginClientDto?> Handle(OriginClientQuery request, CancellationToken cancellationToken)
    {
        try
        {
            return await _platformConfigApi.GetOriginClient(request.ClientCode, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (ApiException apiEx) when (apiEx.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}
