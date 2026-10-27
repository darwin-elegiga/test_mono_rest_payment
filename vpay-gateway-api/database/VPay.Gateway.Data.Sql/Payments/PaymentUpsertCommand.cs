using System.Data;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Gateway.Data.Connection;

namespace VPay.Gateway.Data.Sql.Payments;

public class PaymentUpsertCommand : IRequest<Payment>
{
    public PaymentUpsertCommand(string clientCode, string clientPaymentId, decimal currentBalance)
    {
        ClientCode = clientCode;
        ClientPaymentId = clientPaymentId;
        CurrentBalance = currentBalance;
    }

    public string ClientCode { get; }

    public string ClientPaymentId { get; }

    public decimal CurrentBalance { get; }
}

public class PaymentUpsertCommandHandler : IRequestHandler<PaymentUpsertCommand, Payment>
{
    private readonly IDataConnection<SqlConnection> _connection;

    public PaymentUpsertCommandHandler(IDataConnection<SqlConnection> connection) =>
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));

    public Task<Payment> Handle(PaymentUpsertCommand request, CancellationToken cancellationToken) =>
        _connection.SaveFirstOrDefaultAsync<Payment, SqlConnection>(
            "[dbo].[usp_Payment_Upsert]",
            new
            {
                request.ClientCode,
                request.ClientPaymentId,
                request.CurrentBalance
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
}
