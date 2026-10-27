using MediatR;
using VPay.Gateway.Common.Guidewire;
using VPay.Gateway.Common.Guidewire.Models;
using VPay.Gateway.Data.Sql.Payments;

namespace VPay.Gateway.PaymentProcessor.Consumer.Logic.EventCommands;

public class ProcessDistributeEventCommand : IRequest<PaymentEventProcessResult>
{
    public ProcessDistributeEventCommand(PaymentEvent paymentEvent, GuidewireSettings settings, string clientCode, string clientPaymentId, string paymentStatus)
    {
        PaymentEvent = paymentEvent;
        Settings = settings;
        ClientPaymentId = clientPaymentId;
        PaymentStatus = paymentStatus;
        ClientCode = clientCode;
    }

    public PaymentEvent PaymentEvent { get; }

    public GuidewireSettings Settings { get; }

    public string ClientPaymentId { get; }

    public string PaymentStatus { get; }

    public string ClientCode { get; }
}

public class ProcessDistributeEventCommandHandler : IRequestHandler<ProcessDistributeEventCommand, PaymentEventProcessResult>
{
    private readonly IMediator _mediator;
    private readonly ILogger<ProcessDistributeEventCommandHandler> _logger;

    public ProcessDistributeEventCommandHandler(IMediator mediator, ILogger<ProcessDistributeEventCommandHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<PaymentEventProcessResult> Handle(ProcessDistributeEventCommand request, CancellationToken cancellationToken)
    {
        if (request.PaymentStatus == nameof(GuidewirePaymentStatus.Issued) && request.PaymentEvent.EventPaymentType == "CLNPF")
        {
            return await ProcessPaymentEventClearedForNonPayment(request.PaymentEvent, request.ClientCode, request.ClientPaymentId, request.PaymentStatus, request.Settings, cancellationToken).ConfigureAwait(false);
        }

        string note = request.PaymentEvent.EventStatus switch
        {
            "DISTFAX" => "Payment Faxed",
            "DISTUSPS" => "Payment Mailed - USPS",
            "DISTFEDEX" => "Payment Mailed - FedEx",
            "DIST835PTL" => "Payment Posted",
            _ => "Payment Sent"
        };

        if (!string.IsNullOrWhiteSpace(request.PaymentEvent.EventTrackingNumber))
        {
            note += $" - Tracking Number: {request.PaymentEvent.EventTrackingNumber}";
        }

        IRequest<GuidewireResult> command;

        if (request.PaymentStatus == nameof(GuidewirePaymentStatus.Requested))
        {
            command = new SendPaymentStatusChangeCommand(GuidewirePaymentStatus.Issued, request.ClientPaymentId, note, request.Settings);
        }
        else
        {
            command = new SendPaymentNoteCommand(request.ClientPaymentId, note, request.Settings);
        }

        var message = await _mediator.Send(command, cancellationToken).ConfigureAwait(false);
        _logger.LogGuidewireResult(message, request.PaymentStatus);

        if (message.Status == "SUCCESS")
        {
            if (request.PaymentEvent.EventPaymentType == "CLNPF")
            {
                var payment = await _mediator.Send(new PaymentUpdateStatusCommand(request.PaymentEvent.PaymentId, nameof(GuidewirePaymentStatus.Issued), request.PaymentStatus), cancellationToken).ConfigureAwait(false);
                return await ProcessPaymentEventClearedForNonPayment(request.PaymentEvent, request.ClientCode, payment.ClientPaymentId, payment.LastSentStatus, request.Settings, cancellationToken).ConfigureAwait(false);
            }

            await _mediator.Send(new PaymentEventUpdateStatusCommand(request.PaymentEvent.Id, PaymentNotificationStatus.Completed.Name, request.PaymentEvent.CurrentStatus), cancellationToken).ConfigureAwait(false);
            if (command is SendPaymentStatusChangeCommand)
            {
                var payment = await _mediator.Send(new PaymentUpdateStatusCommand(request.PaymentEvent.PaymentId, nameof(GuidewirePaymentStatus.Issued), request.PaymentStatus), cancellationToken).ConfigureAwait(false);
                return new PaymentEventProcessResult(true, payment.LastSentStatus);
            }

            return new PaymentEventProcessResult(true, request.PaymentStatus);
        }

        await _mediator.Send(new PaymentEventUpdateStatusCommand(request.PaymentEvent.Id, PaymentNotificationStatus.Failed.Name, request.PaymentEvent.CurrentStatus), cancellationToken).ConfigureAwait(false);

        return new PaymentEventProcessResult(false, request.PaymentStatus);
    }

    private async Task<PaymentEventProcessResult> ProcessPaymentEventClearedForNonPayment(PaymentEvent evt, string clientCode, string clientPaymentId, string paymentStatus, GuidewireSettings settings, CancellationToken cancellationToken)
    {
        var message = await _mediator.Send(new SendPaymentStatusChangeCommand(GuidewirePaymentStatus.Cleared, clientPaymentId, "Payment Settled", settings), cancellationToken).ConfigureAwait(false);
        _logger.LogGuidewireResult(message, paymentStatus);

        if (message.Status == "SUCCESS")
        {
            await _mediator.Send(new PaymentEventUpdateStatusCommand(evt.Id, PaymentNotificationStatus.Completed.Name, evt.CurrentStatus), cancellationToken).ConfigureAwait(false);
            var payment = await _mediator.Send(new PaymentUpdateStatusCommand(evt.PaymentId, nameof(GuidewirePaymentStatus.Cleared), paymentStatus), cancellationToken).ConfigureAwait(false);

            return new PaymentEventProcessResult(true, payment.LastSentStatus);
        }

        await _mediator.Send(new PaymentEventUpdateStatusCommand(evt.Id, PaymentNotificationStatus.Failed.Name, evt.CurrentStatus), cancellationToken).ConfigureAwait(false);

        return new PaymentEventProcessResult(false, paymentStatus);
    }
}
