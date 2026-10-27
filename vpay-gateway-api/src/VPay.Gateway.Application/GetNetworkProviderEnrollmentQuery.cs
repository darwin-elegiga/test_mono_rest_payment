using MediatR;
using Microsoft.Extensions.Logging;
using VPay.Gateway.Common.Preferencing;
using VPay.Gateway.Common.Preferencing.Models;

namespace VPay.Gateway.Application;

public class GetNetworkProviderEnrollmentQuery : IRequest<NetworkProviderEnrollmentDto>
{
    public string NetworkName { get; set; } = string.Empty;
    public string ProviderTin { get; set; } = string.Empty;
    public string EnrollmentType { get; set; } = string.Empty;
    public DateTime? DateWhenActive { get; set; }

    public GetNetworkProviderEnrollmentQuery(
        string networkName,
        string providerTin,
        string enrollmentType,
        DateTime? dateWhenActive = null)
    {
        NetworkName = networkName;
        ProviderTin = providerTin;
        EnrollmentType = enrollmentType;
        DateWhenActive = dateWhenActive;
    }
}

public partial class GetNetworkProviderEnrollmentQueryHandler : IRequestHandler<GetNetworkProviderEnrollmentQuery, NetworkProviderEnrollmentDto?>
{
    private readonly IPreferencingApi _preferencingApi;
    private readonly ILogger<GetNetworkProviderEnrollmentQueryHandler> _logger;

    public GetNetworkProviderEnrollmentQueryHandler(
        IPreferencingApi preferencingApi,
        ILogger<GetNetworkProviderEnrollmentQueryHandler> logger)
    {
        _preferencingApi = preferencingApi ?? throw new ArgumentNullException(nameof(preferencingApi));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<NetworkProviderEnrollmentDto?> Handle(GetNetworkProviderEnrollmentQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            NetworkProviderEnrollmentDto network = await _preferencingApi
                .GetNetworkProviderEnrollment(request.NetworkName, request.ProviderTin, request.EnrollmentType, request.DateWhenActive, cancellationToken)
                .ConfigureAwait(false);

            Log.NetworkProviderEnrollmentQueryRequested(_logger, request.NetworkName, request.ProviderTin, request.EnrollmentType);

            return network;
        }
        catch (Refit.ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            Log.NetworkProviderEnrollmentQueryError(_logger, ex, request.NetworkName, request.ProviderTin, request.EnrollmentType);
            throw;
        }
    }

    private static partial class Log
    {
        [LoggerMessage(
            EventId = 0,
            Level = LogLevel.Error,
            Message = "ERROR Network Provider Enrollment: Network = {NetworkName}, Provider = {ProviderTin}, Enrollment = {EnrollmentType}")]
        public static partial void NetworkProviderEnrollmentQueryError(ILogger logger, Exception ex, string networkName, string providerTin, string enrollmentType);

        [LoggerMessage(
            EventId = 10,
            Level = LogLevel.Information,
            Message = "REQUESTED Network Provider Enrollment: Network = {NetworkName}, Provider = {ProviderTin}, Enrollment = {EnrollmentType}")]
        public static partial void NetworkProviderEnrollmentQueryRequested(ILogger logger, string networkName, string providerTin, string enrollmentType);
    }
}
