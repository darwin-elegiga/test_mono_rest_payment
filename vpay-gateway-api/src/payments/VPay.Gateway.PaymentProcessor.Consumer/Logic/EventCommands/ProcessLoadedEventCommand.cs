using MediatR;
using VPay.Gateway.Common.Guidewire;
using VPay.Gateway.Common.Guidewire.Models;
using VPay.Gateway.Data.Sql.Payments;

namespace VPay.Gateway.PaymentProcessor.Consumer.Logic.EventCommands;

public class ProcessLoadedEventCommand : IRequest<PaymentEventProcessResult>
{
    public ProcessLoadedEventCommand(PaymentEvent paymentEvent, GuidewireSettings settings, string clientPaymentId, string paymentStatus)
    {
        PaymentEvent = paymentEvent;
        Settings = settings;
        ClientPaymentId = clientPaymentId;
        PaymentStatus = paymentStatus;
    }

    public PaymentEvent PaymentEvent { get; }

    public GuidewireSettings Settings { get; }

    public string ClientPaymentId { get; }

    public string PaymentStatus { get; }
}

public class ProcessLoadedEventCommandHandler : IRequestHandler<ProcessLoadedEventCommand, PaymentEventProcessResult>
{
    private readonly IMediator _mediator;
    private readonly ILogger<ProcessLoadedEventCommandHandler> _logger;

    public ProcessLoadedEventCommandHandler(IMediator mediator, ILogger<ProcessLoadedEventCommandHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<PaymentEventProcessResult> Handle(ProcessLoadedEventCommand request, CancellationToken cancellationToken)
    {
        string note = "Payment Requested/Loaded";

        switch (request.PaymentEvent.EventPaymentType)
        {
            case "CLCHK":
                note += " - Check";
                break;

            case "CLVCP":
                note += " - Card";
                break;

            case "CLEFT":
            case "CLSDA":
                note += " - ACH";
                break;

            case "CLDPY":
                note += " - Direct Deposit";
                break;

            case "CLNPF":
                note += " - No Payment";
                break;
        }

        var message = await _mediator.Send(new SendPaymentNoteCommand(request.ClientPaymentId, note, request.Settings), cancellationToken).ConfigureAwait(false);

        if (message.Status == "SUCCESS")
        {
            _logger.LogInformation("Payment Event result for status update is: {Result} - {ResultMessage}", message.Status, message.Message);

            await _mediator.Send(new PaymentEventUpdateStatusCommand(request.PaymentEvent.Id, PaymentNotificationStatus.Completed.Name, request.PaymentEvent.CurrentStatus), cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(request.PaymentStatus))
            {
                var payment = await _mediator.Send(new PaymentUpdateStatusCommand(request.PaymentEvent.PaymentId, nameof(GuidewirePaymentStatus.Requested), request.PaymentStatus), cancellationToken).ConfigureAwait(false);
                return new PaymentEventProcessResult(true, payment.LastSentStatus);
            }

            return new PaymentEventProcessResult(true, request.PaymentStatus);
        }
        else
        {
            _logger.LogError("Payment Event result for status update is: {Result} - {ResultMessage}", message.Status, message.Message);
            await _mediator.Send(new PaymentEventUpdateStatusCommand(request.PaymentEvent.Id, PaymentNotificationStatus.Failed.Name, request.PaymentEvent.CurrentStatus), cancellationToken).ConfigureAwait(false);
        }

        return new PaymentEventProcessResult(false, request.PaymentStatus);
    }
}
