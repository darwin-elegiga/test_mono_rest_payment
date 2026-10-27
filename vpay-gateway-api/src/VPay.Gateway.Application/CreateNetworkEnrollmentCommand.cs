using MediatR;
using Microsoft.Extensions.Logging;
using VPay.Gateway.Common.Preferencing;
using VPay.Gateway.Common.Preferencing.Models;

namespace VPay.Gateway.Application;

public record CreateNetworkEnrollmentCommand
(
    NetworkEnrollmentRequest Records
) : IRequest;

public partial class CreateNetworkEnrollmentCommandHandler : IRequestHandler<CreateNetworkEnrollmentCommand>
{
    private readonly IMediator _mediator;
    private readonly IPreferencingApi _preferencingApi;
    private readonly ILogger<CreateNetworkEnrollmentCommandHandler> _logger;

    public CreateNetworkEnrollmentCommandHandler(
        IMediator mediator,
        IPreferencingApi preferencingApi,
        ILogger<CreateNetworkEnrollmentCommandHandler> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _preferencingApi = preferencingApi ?? throw new ArgumentNullException(nameof(preferencingApi));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Handle(CreateNetworkEnrollmentCommand request, CancellationToken cancellationToken)
    {
        NetworkEnrollmentRequestDto payload = request.Records.Payload;

        var networkName = payload.Header.NetworkPartner;
        var correlationId = payload.Header.CorrelationId;
        var enrollmentCount = payload.Header.Count;

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
        });

        try
        {
            await _preferencingApi
                .EnrollNetworkProviders(networkName, payload, cancellationToken)
                .ConfigureAwait(false);

            Log.NetworkEnrollmentRequested(_logger, correlationId, networkName, enrollmentCount);
        }
        catch (Exception ex)
        {
            Log.NetworkEnrollmentError(_logger, ex, correlationId, networkName, enrollmentCount);
            throw;
        }
    }

    private static partial class Log
    {
        [LoggerMessage(
            EventId = 0,
            Level = LogLevel.Error,
            Message = "ERROR Network Enrollment: Correlation = {CorrelationId}, Network = {NetworkName}, Count = {Count}")]
        public static partial void NetworkEnrollmentError(ILogger logger, Exception ex, string correlationId, string networkName, int count);

        [LoggerMessage(
            EventId = 10,
            Level = LogLevel.Information,
            Message = "REQUESTED Network Enrollment: Correlation = {CorrelationId}, Network = {NetworkName}, Count = {Count}")]
        public static partial void NetworkEnrollmentRequested(ILogger logger, string correlationId, string networkName, int count);
    }
}
