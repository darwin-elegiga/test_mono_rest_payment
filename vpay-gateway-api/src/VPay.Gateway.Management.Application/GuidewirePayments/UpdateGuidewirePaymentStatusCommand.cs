using MediatR;
using Microsoft.Extensions.Logging;
using VPay.Gateway.Common.Guidewire;
using VPay.Gateway.Common.Guidewire.Models;
using VPay.Gateway.Common.Kubera;
using VPay.Gateway.Common.PlatformConfig;
using VPay.Gateway.Data.Sql.Payments;

namespace VPay.Gateway.Management.Application.GuidewirePayments;

public class UpdateGuidewirePaymentStatusCommand : IRequest<GuidewireResult>
{
    public UpdateGuidewirePaymentStatusCommand(string clientCode, string clientPaymentId, GuidewirePaymentStatus status, string note)
    {
        ClientCode = clientCode;
        ClientPaymentId = clientPaymentId;
        Status = status;
        Note = note;
    }

    public string ClientCode { get; }

    public string ClientPaymentId { get; }

    public GuidewirePaymentStatus Status { get; }

    public string Note { get; }
}

public class UpdateGuidewirePaymentStatusCommandHandler : IRequestHandler<UpdateGuidewirePaymentStatusCommand, GuidewireResult>
{
    private readonly IMediator _mediator;
    private readonly ILogger<UpdateGuidewirePaymentStatusCommandHandler> _logger;

    public UpdateGuidewirePaymentStatusCommandHandler(IMediator mediator, ILogger<UpdateGuidewirePaymentStatusCommandHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<GuidewireResult> Handle(UpdateGuidewirePaymentStatusCommand request, CancellationToken cancellationToken)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["ClientCode"] = request.ClientCode,
            ["PaymentId"] = request.ClientPaymentId
        });

        // get the configuration for where to send messages
        var externalClientConfiguration = await _mediator.Send(new ExternalClientConfigurationQuery(request.ClientCode, DateTime.Today), cancellationToken).ConfigureAwait(false);
        if (externalClientConfiguration == null)
        {
            _logger.LogInformation("Client '{ClientCode}' is not configured to send to Guidewire", request.ClientCode);
            throw new ArgumentException($"Client '{request.ClientCode}' is not configured to send to Guidewire", nameof(request.ClientCode));
        }

        var settings = externalClientConfiguration.ToGuidewireSettings();

        var payment = await _mediator.Send(new PaymentQuery(request.ClientCode, request.ClientPaymentId), cancellationToken).ConfigureAwait(false);
        if (payment == null)
        {
            //look up the payment
            var txns = await _mediator.Send(new GetTransactionsByPaymentIdQuery(request.ClientCode, request.ClientPaymentId), cancellationToken).ConfigureAwait(false);

            if (txns == null || txns.Length == 0)
            {
                _logger.LogInformation("Payment not found for Client '{ClientCode}', Payment Id '{PaymentId}'", request.ClientCode, request.ClientPaymentId);
                return new GuidewireResult { Status = "FAILED", Message = "Could not Locate Payment" };
            }

            //insert the payment if found
            var currentBalance = txns.OrderByDescending(x => x.DateLoaded).First().CurrentBalance;
            payment = await _mediator.Send(new PaymentUpsertCommand(request.ClientCode, request.ClientPaymentId, currentBalance), cancellationToken).ConfigureAwait(false);
        }

        var message = await _mediator.Send(new SendPaymentStatusChangeCommand(request.Status, request.ClientPaymentId, request.Note, settings)).ConfigureAwait(false);
        _logger.LogGuidewireResult(message, payment.LastSentStatus);

        if (message.Status == "SUCCESS")
        {
            await _mediator.Send(new PaymentUpdateStatusCommand(payment.Id, request.Status.ToString(), payment.LastSentStatus), cancellationToken).ConfigureAwait(false);
        }

        return message;
    }
}
