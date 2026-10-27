using System.Data;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Gateway.Data.Connection;

namespace VPay.Gateway.Data.Sql.Payments;

public class PaymentForTransactionQuery : IRequest<Payment?>
{
    public PaymentForTransactionQuery(long transactionId)
    {
        TransactionId = transactionId;
    }

    public long TransactionId { get; }
}

public class PaymentForTransactionQueryHandler : IRequestHandler<PaymentForTransactionQuery, Payment?>
{
    private readonly IDataConnection<SqlConnection> _connection;

    public PaymentForTransactionQueryHandler(IDataConnection<SqlConnection> connection) =>
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));

    public Task<Payment?> Handle(PaymentForTransactionQuery request, CancellationToken cancellationToken) =>
        _connection.GetFirstOrDefaultAsync<Payment?, SqlConnection>(
            "[dbo].[usp_Payment_GetByTransactionId]",
            new
            {
                request.TransactionId
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
}
