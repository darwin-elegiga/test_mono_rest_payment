using System.Data;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Gateway.Data.Connection;

namespace VPay.Gateway.Data.Sql.Payments;

public class PaymentEventInsertCommand : IRequest<PaymentEvent>
{
    public PaymentEventInsertCommand(
        long paymentId,
        long transactionId,
        long transactionEventId,
        string eventStatus,
        DateTime eventDateTime,
        string eventReasonCode,
        string eventReasonDescription,
        string eventPaymentType,
        string eventTrackingNumber
        )
    {
        PaymentId = paymentId;
        TransactionId = transactionId;
        TransactionEventId = transactionEventId;
        EventStatus = eventStatus;
        EventDateTime = eventDateTime;
        EventReasonCode = eventReasonCode;
        EventReasonDescription = eventReasonDescription;
        EventPaymentType = eventPaymentType;
        EventTrackingNumber = eventTrackingNumber;
    }

    public long PaymentId { get; }

    public long TransactionId { get; }

    public long TransactionEventId { get; }

    public string EventStatus { get; }

    public DateTime EventDateTime { get; set; }

    public string EventReasonCode { get; }

    public string EventReasonDescription { get; }

    public string EventPaymentType { get; }

    public string EventTrackingNumber { get; }
}

public class PaymentEventInsertCommandHandler : IRequestHandler<PaymentEventInsertCommand, PaymentEvent>
{
    private readonly IDataConnection<SqlConnection> _connection;

    public PaymentEventInsertCommandHandler(IDataConnection<SqlConnection> connection) =>
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));

    public Task<PaymentEvent> Handle(PaymentEventInsertCommand request, CancellationToken cancellationToken) =>
        _connection.SaveFirstOrDefaultAsync<PaymentEvent, SqlConnection>(
            "[dbo].[usp_PaymentEvent_Insert]",
            new
            {
                request.PaymentId,
                request.TransactionId,
                request.TransactionEventId,
                request.EventStatus,
                request.EventDateTime,
                request.EventReasonCode,
                request.EventReasonDescription,
                request.EventPaymentType,
                request.EventTrackingNumber
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
}
