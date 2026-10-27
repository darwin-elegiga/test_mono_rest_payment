using MediatR;
using Microsoft.Extensions.Logging;
using VPay.Gateway.Common.Preferencing;
using VPay.Gateway.Common.Preferencing.Models;

namespace VPay.Gateway.Application;

public class GetNetworkQuery : IRequest<NetworkDto>
{
    public string NetworkName { get; set; } = string.Empty;
    public bool? ActiveStatus { get; set; }

    public GetNetworkQuery(string networkName, bool? activeStatus = null)
    {
        NetworkName = networkName;
        ActiveStatus = activeStatus;
    }
}

public partial class GetNetworkByNameQueryHandler : IRequestHandler<GetNetworkQuery, NetworkDto?>
{
    private readonly IPreferencingApi _preferencingApi;
    private readonly ILogger<GetNetworkByNameQueryHandler> _logger;

    public GetNetworkByNameQueryHandler(
        IPreferencingApi preferencingApi,
        ILogger<GetNetworkByNameQueryHandler> logger)
    {
        _preferencingApi = preferencingApi ?? throw new ArgumentNullException(nameof(preferencingApi));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<NetworkDto?> Handle(GetNetworkQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        string status = request.ActiveStatus == null
            ? "Active or Inactive"
            : (request.ActiveStatus.Value ? "Active" : "Inactive");

        try
        {
            NetworkDto network = await _preferencingApi
                .GetNetwork(request.NetworkName, request.ActiveStatus, cancellationToken)
                .ConfigureAwait(false);

            Log.NetworkQueryRequested(_logger, request.NetworkName, status);

            return network;
        }
        catch (Refit.ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            Log.NetworkQueryError(_logger, ex, request.NetworkName, status);
            throw;
        }
    }

    private static partial class Log
    {
        [LoggerMessage(
            EventId = 0,
            Level = LogLevel.Error,
            Message = "ERROR Network: Network = {NetworkName}, Status = {Status}")]
        public static partial void NetworkQueryError(ILogger logger, Exception ex, string networkName, string status);

        [LoggerMessage(
            EventId = 10,
            Level = LogLevel.Information,
            Message = "REQUESTED Network: Network = {NetworkName}, Status = {Status}")]
        public static partial void NetworkQueryRequested(ILogger logger, string networkName, string status);
    }
}
