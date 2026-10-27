using System.Data;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Gateway.Data.Connection;

namespace VPay.Gateway.Data.Sql.Payments;

public class PaymentQuery : IRequest<Payment?>
{
    public PaymentQuery(string clientCode, string clientPaymentId)
    {
        ClientCode = clientCode;
        ClientPaymentId = clientPaymentId;
    }

    public string ClientCode { get; }

    public string ClientPaymentId { get; }
}

public class PaymentQueryHandler : IRequestHandler<PaymentQuery, Payment?>
{
    private readonly IDataConnection<SqlConnection> _connection;

    public PaymentQueryHandler(IDataConnection<SqlConnection> connection) =>
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));

    public Task<Payment?> Handle(PaymentQuery request, CancellationToken cancellationToken) =>
        _connection.GetFirstOrDefaultAsync<Payment?, SqlConnection>(
            "[dbo].[usp_Payment_GetByClientCodeAndPaymentId]",
            new
            {
                request.ClientCode,
                request.ClientPaymentId
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
}
