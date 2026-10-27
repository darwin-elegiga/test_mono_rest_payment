using System.Data;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Gateway.Data.Connection;

namespace VPay.Gateway.Data.Sql.Payments;

public class PaymentEventUpdateStatusCommand : IRequest<PaymentEvent>
{
    public PaymentEventUpdateStatusCommand(long paymentEventId, string status, string previousStatus)
    {
        PaymentEventId = paymentEventId;
        Status = status;
        PreviousStatus = previousStatus;
    }

    public long PaymentEventId { get; }

    public string Status { get; }

    public string PreviousStatus { get; }
}

public class PaymentEventUpdateStatusCommandHandler : IRequestHandler<PaymentEventUpdateStatusCommand, PaymentEvent>
{
    private readonly IDataConnection<SqlConnection> _connection;

    public PaymentEventUpdateStatusCommandHandler(IDataConnection<SqlConnection> connection) =>
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));

    public Task<PaymentEvent> Handle(PaymentEventUpdateStatusCommand request, CancellationToken cancellationToken) =>
        _connection.SaveFirstOrDefaultAsync<PaymentEvent, SqlConnection>(
            "[dbo].[usp_PaymentEvent_UpdateStatus]",
            new
            {
                request.PaymentEventId,
                request.Status,
                request.PreviousStatus
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
}
