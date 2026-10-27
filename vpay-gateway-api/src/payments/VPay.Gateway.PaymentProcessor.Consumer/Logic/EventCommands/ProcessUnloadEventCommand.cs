using MediatR;
using VPay.Gateway.Common.Guidewire;
using VPay.Gateway.Common.Guidewire.Models;
using VPay.Gateway.Data.Sql.Payments;

namespace VPay.Gateway.PaymentProcessor.Consumer.Logic.EventCommands;

public record ProcessUnloadEventCommand(
    PaymentEvent PaymentEvent,
    GuidewireSettings Settings,
    string ClientCode,
    string ClientPaymentId,
    string PaymentStatus) : IRequest<PaymentEventProcessResult>;

public class ProcessUnloadEventCommandHandler : IRequestHandler<ProcessUnloadEventCommand, PaymentEventProcessResult>
{
    private readonly IMediator _mediator;
    private readonly ILogger<ProcessUnloadEventCommandHandler> _logger;

    public ProcessUnloadEventCommandHandler(IMediator mediator, ILogger<ProcessUnloadEventCommandHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<PaymentEventProcessResult> Handle(ProcessUnloadEventCommand request, CancellationToken cancellationToken)
    {
        using var loggerScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["reasonCode"] = request.PaymentEvent.EventReasonCode
        });

        var unloadProcess = await _mediator.Send(new GuidewireUnloadProcessQuery(request.PaymentEvent.EventReasonCode), cancellationToken).ConfigureAwait(false);
        return unloadProcess?.ProcessType switch
        {
            GuidewireUnloadProcessType.DoNothing => await ProcessSkippedEvent(request, cancellationToken).ConfigureAwait(false),
            GuidewireUnloadProcessType.Cleared => await ProcessClearedEvent(request, unloadProcess, cancellationToken).ConfigureAwait(false),
            GuidewireUnloadProcessType.UnloadReissue => await ProcessUnloadReissueEvent(request, unloadProcess, cancellationToken).ConfigureAwait(false),
            GuidewireUnloadProcessType.UnloadSendback => await ProcessUnloadSendbackEvent(request, unloadProcess, cancellationToken).ConfigureAwait(false),
            _ => await ProcessMissingConfiguration(request, cancellationToken).ConfigureAwait(false)
        };
    }

    private async Task<PaymentEventProcessResult> ProcessSkippedEvent(ProcessUnloadEventCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Payment Event skipped: {EventStatus}", request.PaymentEvent.EventStatus);
        await _mediator.Send(new PaymentEventUpdateStatusCommand(request.PaymentEvent.Id, PaymentNotificationStatus.Skipped.Name, request.PaymentEvent.CurrentStatus), cancellationToken).ConfigureAwait(false);

        return new PaymentEventProcessResult(true, request.PaymentStatus);
    }

    private async Task<PaymentEventProcessResult> ProcessClearedEvent(ProcessUnloadEventCommand request, GuidewireUnloadProcess unloadProcess, CancellationToken cancellationToken)
    {
        if (request.PaymentEvent.EventStatus == "UNLOAD")
        {
            var clearCommand = new SendPaymentStatusChangeCommand(GuidewirePaymentStatus.Cleared, request.ClientPaymentId, $"UNLOAD {unloadProcess.ReasonCode} - {unloadProcess.Comment}", request.Settings);
            var message = await _mediator.Send(clearCommand, cancellationToken).ConfigureAwait(false);
            _logger.LogGuidewireResult(message, request.PaymentStatus);

            if (message.Status == "SUCCESS")
            {
                await _mediator.Send(new PaymentEventUpdateStatusCommand(request.PaymentEvent.Id, PaymentNotificationStatus.Completed.Name, request.PaymentEvent.CurrentStatus), cancellationToken).ConfigureAwait(false);

                var payment = await _mediator.Send(new PaymentUpdateStatusCommand(request.PaymentEvent.PaymentId, clearCommand.PaymentStatus.ToString(), request.PaymentStatus), cancellationToken).ConfigureAwait(false);
                return new PaymentEventProcessResult(true, payment.LastSentStatus);
            }

            await _mediator.Send(new PaymentEventUpdateStatusCommand(request.PaymentEvent.Id, PaymentNotificationStatus.Failed.Name, request.PaymentEvent.CurrentStatus), cancellationToken).ConfigureAwait(false);
            return new PaymentEventProcessResult(false, request.PaymentStatus);
        }

        return await ProcessSkippedEvent(request, cancellationToken).ConfigureAwait(false);
    }

    private async Task<PaymentEventProcessResult> ProcessUnloadReissueEvent(ProcessUnloadEventCommand request, GuidewireUnloadProcess unloadProcess, CancellationToken cancellationToken)
    {
        if (request.PaymentEvent.EventStatus == "UNLOAD")
        {
            var noteCommand = new SendPaymentNoteCommand(request.ClientPaymentId, $"UNLOAD {unloadProcess.ReasonCode} - {unloadProcess.Comment}", request.Settings);
            var message = await _mediator.Send(noteCommand, cancellationToken).ConfigureAwait(false);
            _logger.LogGuidewireResult(message, request.PaymentStatus);

            if (message.Status == "SUCCESS")
            {
                await _mediator.Send(new PaymentEventUpdateStatusCommand(request.PaymentEvent.Id, PaymentNotificationStatus.Completed.Name, request.PaymentEvent.CurrentStatus), cancellationToken).ConfigureAwait(false);
                return new PaymentEventProcessResult(true, request.PaymentStatus);
            }

            await _mediator.Send(new PaymentEventUpdateStatusCommand(request.PaymentEvent.Id, PaymentNotificationStatus.Failed.Name, request.PaymentEvent.CurrentStatus), cancellationToken).ConfigureAwait(false);
            return new PaymentEventProcessResult(false, request.PaymentStatus);
        }

        return await ProcessSkippedEvent(request, cancellationToken).ConfigureAwait(false);
    }

    private async Task<PaymentEventProcessResult> ProcessUnloadSendbackEvent(ProcessUnloadEventCommand request, GuidewireUnloadProcess unloadProcess, CancellationToken cancellationToken)
    {
        SendPaymentStatusChangeCommand paymentStatusChangeCommand = null;
        var eventStatus = request.PaymentEvent.EventStatus.Trim().ToUpper();
        if (request.PaymentEvent.EventPaymentType.Equals("CLVCP", StringComparison.OrdinalIgnoreCase))
        {
            if (eventStatus == "UNLOAD")
            {
                paymentStatusChangeCommand = new SendPaymentStatusChangeCommand(GuidewirePaymentStatus.Void, request.ClientPaymentId, $"UNLOAD {unloadProcess.ReasonCode} - {unloadProcess.Comment} - Confirmed", request.Settings);
            }
            else if (eventStatus == "UNLOADS" || eventStatus == "UNLOADSF")
            {
                paymentStatusChangeCommand = new SendPaymentStatusChangeCommand(GuidewirePaymentStatus.PendingVoid, request.ClientPaymentId, $"UNLOAD {unloadProcess.ReasonCode} - {unloadProcess.Comment} - Requested", request.Settings);
            }
        }
        else if (request.PaymentEvent.EventPaymentType.Equals("CLCHK", StringComparison.OrdinalIgnoreCase))
        {
            if (eventStatus == "STOPPAY")
            {
                paymentStatusChangeCommand = new SendPaymentStatusChangeCommand(GuidewirePaymentStatus.PendingStop, request.ClientPaymentId, $"STOPPAY {unloadProcess.ReasonCode} - {unloadProcess.Comment} - Requested", request.Settings);
            }
            else if (eventStatus == "UNLOAD")
            {
                paymentStatusChangeCommand = new SendPaymentStatusChangeCommand(GuidewirePaymentStatus.Stop, request.ClientPaymentId, $"STOPPAY {unloadProcess.ReasonCode} - {unloadProcess.Comment} - Confirmed", request.Settings);
            }
            else
            {
                return await ProcessSkippedEvent(request, cancellationToken).ConfigureAwait(false);
            }
        }

        if (paymentStatusChangeCommand != null)
        {
            var message = await _mediator.Send(paymentStatusChangeCommand, cancellationToken).ConfigureAwait(false);
            _logger.LogGuidewireResult(message, request.PaymentStatus);

            if (message.Status == "SUCCESS")
            {
                await _mediator.Send(new PaymentEventUpdateStatusCommand(request.PaymentEvent.Id, PaymentNotificationStatus.Completed.Name, request.PaymentEvent.CurrentStatus), cancellationToken).ConfigureAwait(false);
                return new PaymentEventProcessResult(true, request.PaymentStatus);
            }
        }

        await _mediator.Send(new PaymentEventUpdateStatusCommand(request.PaymentEvent.Id, PaymentNotificationStatus.Failed.Name, request.PaymentEvent.CurrentStatus), cancellationToken).ConfigureAwait(false);
        return new PaymentEventProcessResult(false, request.PaymentStatus);
    }

    private async Task<PaymentEventProcessResult> ProcessMissingConfiguration(ProcessUnloadEventCommand request, CancellationToken cancellationToken)
    {
        _logger.LogError("Payment Event failed.  Missing configuration for: {Status} - {reasonCode}", request.PaymentEvent.EventStatus, request.PaymentEvent.EventReasonCode);
        await _mediator.Send(new PaymentEventUpdateStatusCommand(request.PaymentEvent.Id, PaymentNotificationStatus.Failed.Name, request.PaymentEvent.CurrentStatus), cancellationToken).ConfigureAwait(false);
        return new PaymentEventProcessResult(false, request.PaymentStatus);
    }
}
