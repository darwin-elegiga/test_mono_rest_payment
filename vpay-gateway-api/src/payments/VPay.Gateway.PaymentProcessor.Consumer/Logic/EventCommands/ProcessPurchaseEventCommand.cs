using MediatR;
using VPay.Gateway.Common.Guidewire;
using VPay.Gateway.Common.Guidewire.Models;
using VPay.Gateway.Data.Sql.Payments;

namespace VPay.Gateway.PaymentProcessor.Consumer.Logic.EventCommands;

public class ProcessPurchaseEventCommand : IRequest<PaymentEventProcessResult>
{
    public ProcessPurchaseEventCommand(PaymentEvent paymentEvent, GuidewireSettings settings, string clientCode, string clientPaymentId, string paymentStatus, decimal currentBalance)
    {
        PaymentEvent = paymentEvent;
        Settings = settings;
        ClientPaymentId = clientPaymentId;
        PaymentStatus = paymentStatus;
        CurrentBalance = currentBalance;
        ClientCode = clientCode;
    }

    public PaymentEvent PaymentEvent { get; }

    public GuidewireSettings Settings { get; }

    public string ClientPaymentId { get; }

    public string PaymentStatus { get; }

    public decimal CurrentBalance { get; }

    public string ClientCode { get; }
}

public class ProcessPurchaseEventCommandHandler : IRequestHandler<ProcessPurchaseEventCommand, PaymentEventProcessResult>
{
    private readonly IMediator _mediator;
    private readonly ILogger<ProcessPurchaseEventCommandHandler> _logger;

    public ProcessPurchaseEventCommandHandler(IMediator mediator, ILogger<ProcessPurchaseEventCommandHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<PaymentEventProcessResult> Handle(ProcessPurchaseEventCommand request, CancellationToken cancellationToken)
    {
        if (request.CurrentBalance == 0m)
        {
            var message = await _mediator.Send(new SendPaymentStatusChangeCommand(GuidewirePaymentStatus.Cleared, request.ClientPaymentId, "Payment Settled", request.Settings), cancellationToken).ConfigureAwait(false);
            _logger.LogGuidewireResult(message, request.PaymentStatus);

            if (message.Status == "SUCCESS")
            {
                await _mediator.Send(new PaymentEventUpdateStatusCommand(request.PaymentEvent.Id, PaymentNotificationStatus.Completed.Name, request.PaymentEvent.CurrentStatus), cancellationToken).ConfigureAwait(false);
                var payment = await _mediator.Send(new PaymentUpdateStatusCommand(request.PaymentEvent.PaymentId, nameof(GuidewirePaymentStatus.Cleared), request.PaymentStatus), cancellationToken).ConfigureAwait(false);

                return new PaymentEventProcessResult(true, payment.LastSentStatus);
            }
            else
            {
                await _mediator.Send(new PaymentEventUpdateStatusCommand(request.PaymentEvent.Id, PaymentNotificationStatus.Failed.Name, request.PaymentEvent.CurrentStatus), cancellationToken).ConfigureAwait(false);
            }
            return new PaymentEventProcessResult(false, request.PaymentStatus);
        }

        _logger.LogInformation("Payment Event skipped due to having a current balance: {EventStatus}", request.PaymentEvent.EventStatus);
        await _mediator.Send(new PaymentEventUpdateStatusCommand(request.PaymentEvent.Id, PaymentNotificationStatus.Skipped.Name, request.PaymentEvent.CurrentStatus), cancellationToken).ConfigureAwait(false);

        return new PaymentEventProcessResult(true, request.PaymentStatus);
    }
}
