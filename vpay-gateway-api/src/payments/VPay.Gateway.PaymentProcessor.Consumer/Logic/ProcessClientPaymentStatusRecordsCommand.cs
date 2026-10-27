using MediatR;
using VPay.Gateway.Common.Guidewire.Models;
using VPay.Gateway.Common.PlatformConfig;
using VPay.Gateway.Data.Sql.Payments;
using VPay.Gateway.PaymentProcessor.Consumer.Logic.EventCommands;

namespace VPay.Gateway.PaymentProcessor.Consumer.Logic;

public class ProcessClientPaymentStatusRecordsCommand : IRequest<bool>
{
    public ProcessClientPaymentStatusRecordsCommand(string clientCode, string clientPaymentId)
    {
        ClientCode = clientCode;
        ClientPaymentId = clientPaymentId;
    }

    public string ClientCode { get; }

    public string ClientPaymentId { get; }
}

public class ProcessClientPaymentStatusRecordsCommandHandler : IRequestHandler<ProcessClientPaymentStatusRecordsCommand, bool>
{
    private readonly IMediator _mediator;
    private readonly ILogger<ProcessClientPaymentStatusRecordsCommandHandler> _logger;

    public ProcessClientPaymentStatusRecordsCommandHandler(IMediator mediator, ILogger<ProcessClientPaymentStatusRecordsCommandHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<bool> Handle(ProcessClientPaymentStatusRecordsCommand request, CancellationToken cancellationToken)
    {
        var payment = await _mediator.Send(new PaymentQuery(request.ClientCode, request.ClientPaymentId), cancellationToken).ConfigureAwait(false);
        if (payment == null)
        {
            _logger.LogInformation("Payment not found for Client '{ClientCode}', Payment Id '{PaymentId}'", request.ClientCode, request.ClientPaymentId);
            return false;
        }

        // get the configuration for where to send messages
        var externalClientConfiguration = await _mediator.Send(new ExternalClientConfigurationQuery(request.ClientCode, DateTime.Now), cancellationToken).ConfigureAwait(false);
        if (externalClientConfiguration == null)
        {
            _logger.LogInformation("Client '{ClientCode}' is not configured to send to Guidewire", request.ClientCode);
            return false;
        }

        var settings = new GuidewireSettings
        {
            Url = externalClientConfiguration.Url,
            UserName = externalClientConfiguration.Username,
            Password = externalClientConfiguration.Password
        };

        var events = await _mediator.Send(new PaymentEventsQuery(request.ClientCode, request.ClientPaymentId), cancellationToken).ConfigureAwait(false);

        string paymentStatus = payment.LastSentStatus;
        foreach (var evt in events.OrderBy(x => x.EventDateTime))
        {
            var result = await ProcessPaymentEvent(evt, settings, request.ClientCode, payment.ClientPaymentId, paymentStatus, payment.CurrentBalance, cancellationToken).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                return false;
            }

            paymentStatus = result.GuidewireStatus;
        }

        return true;
    }

    private Task<PaymentEventProcessResult> ProcessPaymentEvent(PaymentEvent evt, GuidewireSettings settings, string clientCode, string clientPaymentId, string paymentStatus, decimal currentBalance, CancellationToken cancellationToken)
    {
        using var loggerScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["TransactionEventId"] = evt.TransactionEventId,
            ["TransactionId"] = evt.TransactionId,
            ["PaymentTypeCode"] = evt.EventPaymentType,
            ["EventStatus"] = evt.EventStatus
        });

        switch (evt.CurrentStatus)
        {
            case var e when e.Equals(PaymentNotificationStatus.Completed.Name):
            case var e2 when e2.Equals(PaymentNotificationStatus.Skipped.Name):
            case var e3 when e3.Equals(PaymentNotificationStatus.MarkedCompleted.Name):
                return Task.FromResult(new PaymentEventProcessResult(true, paymentStatus)); //skip the record
        }

        switch (evt.EventStatus.Trim().ToUpper())
        {
            case "LOADED":
                return _mediator.Send(new ProcessLoadedEventCommand(evt, settings, clientPaymentId, paymentStatus), cancellationToken);

            case "DIST835PTL":
            case "DISTFAX":
            case "DISTFEDEX":
            case "DISTUSPS":
                return _mediator.Send(new ProcessDistributeEventCommand(evt, settings, clientCode, clientPaymentId, paymentStatus), cancellationToken);

            case "STOPPAY":
            case "STOPPAY_R":
            case "UNLOADS":
            case "UNLOADSF":
            case "UNLOAD":
                return _mediator.Send(new ProcessUnloadEventCommand(evt, settings, clientCode, clientPaymentId, paymentStatus), cancellationToken);

            //case "UNLOADS":
            //    return _mediator.Send(new ProcessUnloadSEventCommand(evt, settings, clientPaymentId, paymentStatus));

            //case "UNLOADSF":
            //    return _mediator.Send(new ProcessUnloadSFEventCommand(evt, settings, clientPaymentId, paymentStatus));

            case "PURCHASE":
                return _mediator.Send(new ProcessPurchaseEventCommand(evt, settings, clientCode, clientPaymentId, paymentStatus, currentBalance), cancellationToken);

            default:
                return ProcessSkippedEvent(evt, paymentStatus, cancellationToken);
        }
    }

    private async Task<PaymentEventProcessResult> ProcessSkippedEvent(PaymentEvent evt, string paymentStatus, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Payment Event skipped: {EventStatus}", evt.EventStatus);
        await _mediator.Send(new PaymentEventUpdateStatusCommand(evt.Id, PaymentNotificationStatus.Skipped.Name, evt.CurrentStatus), cancellationToken).ConfigureAwait(false);

        return new PaymentEventProcessResult(true, paymentStatus);
    }
}
