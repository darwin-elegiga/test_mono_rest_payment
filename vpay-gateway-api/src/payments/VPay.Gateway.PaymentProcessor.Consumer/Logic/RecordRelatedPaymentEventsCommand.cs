using MediatR;
using VPay.Gateway.Common.Kubera;
using VPay.Gateway.Common.Kubera.Models;
using VPay.Gateway.Common.PlatformConfig;
using VPay.Gateway.Data.Connection;
using VPay.Gateway.Data.Sql.Payments;

namespace VPay.Gateway.PaymentProcessor.Consumer.Logic;

public class RecordRelatedPaymentEventsCommand : IRequest<bool>
{
    public RecordRelatedPaymentEventsCommand(string clientCode, string clientPaymentId)
    {
        ClientCode = clientCode;
        ClientPaymentId = clientPaymentId;
    }

    public string ClientCode { get; }

    public string ClientPaymentId { get; }
}

public class RecordRelatedPaymentEventsCommandHandler : IRequestHandler<RecordRelatedPaymentEventsCommand, bool>
{
    private readonly IMediator _mediator;
    private readonly IDbUnitOfWork _uow;
    private readonly ILogger<RecordRelatedPaymentEventsCommandHandler> _logger;

    public RecordRelatedPaymentEventsCommandHandler(IMediator mediator,
        IDbUnitOfWork unitOfWork,
        ILogger<RecordRelatedPaymentEventsCommandHandler> logger)
    {
        _mediator = mediator;
        _uow = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(RecordRelatedPaymentEventsCommand request, CancellationToken cancellationToken)
    {
        var externalClientConfiguration = await _mediator.Send(new ExternalClientConfigurationQuery(request.ClientCode, DateTime.Today), cancellationToken).ConfigureAwait(false);
        if (externalClientConfiguration == null)
        {
            // no LogError for the client to send to guidewire.  Just ignore the message
            _logger.LogInformation("Received PaymentId = {PaymentId}.  Client '{ClientCode}' is not configured to send to Guidewire", request.ClientPaymentId, request.ClientCode);
            return false;
        }

        var txns = await _mediator.Send(new GetTransactionsByPaymentIdQuery(request.ClientCode, request.ClientPaymentId), cancellationToken).ConfigureAwait(false);

        if (txns == null || txns.Length == 0)
        {
            // no LogError for the client to send to guidewire.  Just ignore the message
            _logger.LogInformation("Received PaymentId = {PaymentId}.  Client '{ClientCode}' could not locate transactions", request.ClientPaymentId, request.ClientCode);
            return false;
        }

        var currentBalance = txns.OrderByDescending(x => x.DateLoaded).First().CurrentBalance;

        using var transaction = await _uow.BeginTransaction().ConfigureAwait(false);

        var insertedPayment = await _mediator.Send(new PaymentUpsertCommand(request.ClientCode, request.ClientPaymentId, currentBalance), cancellationToken).ConfigureAwait(false);

        List<PaymentEventInsertCommand> eventInserts = new List<PaymentEventInsertCommand>();

        foreach (var txn in txns)
        {
            var vals = (txn.Events ?? Enumerable.Empty<TransactionEvent>()).OrderBy(x => x.ProcessedDate)
                .Select(x => new PaymentEventInsertCommand(
                insertedPayment.Id,
                x.TransactionId,
                x.Id,
                x.Status,
                x.ProcessedDate,
                x.ReasonCode,
                x.ReasonDescription,
                txn.PaymentTypeCode,
                x.TrackingNumber
                ));

            eventInserts.AddRange(vals);
        }

        foreach (var eventInsert in eventInserts.OrderBy(x => x.EventDateTime))
        {
            await _mediator.Send(eventInsert, cancellationToken).ConfigureAwait(false);
        }

        transaction.Commit();

        return true;
    }
}
