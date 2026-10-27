using MediatR;
using Microsoft.Extensions.Logging;
using VPay.Gateway.Common.Preferencing;
using VPay.Gateway.Common.Preferencing.Models;

namespace VPay.Gateway.Application;

public class GetNetworkPayorEnrollmentQuery : IRequest<NetworkPayorEnrollmentDto>
{
    public string NetworkName { get; set; } = string.Empty;
    public string ClientCode { get; set; } = string.Empty;
    public string BillingEntityCode { get; set; } = string.Empty;
    public DateTime? DateWhenActive { get; set; }

    public GetNetworkPayorEnrollmentQuery(
        string networkName,
        string clientCode,
        string billingEntityCode,
        DateTime? dateWhenActive = null)
    {
        NetworkName = networkName;
        ClientCode = clientCode;
        BillingEntityCode = billingEntityCode;
        DateWhenActive = dateWhenActive;
    }
}

public partial class GetNetworkPayorEnrollmentQueryHandler : IRequestHandler<GetNetworkPayorEnrollmentQuery, NetworkPayorEnrollmentDto?>
{
    private readonly IPreferencingApi _preferencingApi;
    private readonly ILogger<GetNetworkPayorEnrollmentQueryHandler> _logger;

    public GetNetworkPayorEnrollmentQueryHandler(
        IPreferencingApi preferencingApi,
        ILogger<GetNetworkPayorEnrollmentQueryHandler> logger)
    {
        _preferencingApi = preferencingApi ?? throw new ArgumentNullException(nameof(preferencingApi));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<NetworkPayorEnrollmentDto?> Handle(GetNetworkPayorEnrollmentQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            NetworkPayorEnrollmentDto network = await _preferencingApi
                .GetNetworkPayorEnrollment(request.NetworkName, request.ClientCode, request.BillingEntityCode, request.DateWhenActive, cancellationToken)
                .ConfigureAwait(false);

            Log.NetworkPayorEnrollmentQueryRequested(_logger, request.NetworkName, request.ClientCode, request.BillingEntityCode);

            return network;
        }
        catch (Refit.ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            Log.NetworkPayorEnrollmentQueryError(_logger, ex, request.NetworkName, request.ClientCode, request.BillingEntityCode);
            throw;
        }
    }

    private static partial class Log
    {
        [LoggerMessage(
            EventId = 0,
            Level = LogLevel.Error,
            Message = "ERROR Network Payor Enrollment: Network = {NetworkName}, Client = {ClientCode}, BE = {BillingEntityCode}")]
        public static partial void NetworkPayorEnrollmentQueryError(ILogger logger, Exception ex, string networkName, string clientCode, string billingEntityCode);

        [LoggerMessage(
            EventId = 10,
            Level = LogLevel.Information,
            Message = "REQUESTED Network Payor Enrollment: Network = {NetworkName}, Client = {ClientCode}, BE = {BillingEntityCode}")]
        public static partial void NetworkPayorEnrollmentQueryRequested(ILogger logger, string networkName, string clientCode, string billingEntityCode);
    }
}
