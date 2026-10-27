using System.Data;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Gateway.Data.Connection;

namespace VPay.Gateway.Data.Sql.Payments;

public class PaymentUpdateStatusCommand : IRequest<Payment>
{
    public PaymentUpdateStatusCommand(long paymentId, string status, string previousStatus)
    {
        PaymentId = paymentId;
        Status = status;
        PreviousStatus = previousStatus;
    }

    public long PaymentId { get; }

    public string Status { get; }

    public string PreviousStatus { get; }
}

public class PaymentUpdateStatusCommandHandler : IRequestHandler<PaymentUpdateStatusCommand, Payment>
{
    private readonly IDataConnection<SqlConnection> _connection;

    public PaymentUpdateStatusCommandHandler(IDataConnection<SqlConnection> connection) =>
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));

    public Task<Payment> Handle(PaymentUpdateStatusCommand request, CancellationToken cancellationToken) =>
        _connection.SaveFirstOrDefaultAsync<Payment, SqlConnection>(
            "[dbo].[usp_Payment_UpdateStatus]",
            new
            {
                request.PaymentId,
                request.Status,
                request.PreviousStatus
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
}
