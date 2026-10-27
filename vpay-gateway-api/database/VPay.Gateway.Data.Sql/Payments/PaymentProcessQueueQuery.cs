using System.Data;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Gateway.Data.Connection;

namespace VPay.Gateway.Data.Sql.Payments;

public class PaymentProcessQueueQuery : IRequest<PaymentProcessQueue?>
{
    public PaymentProcessQueueQuery(string clientCode, string clientPaymentId)
    {
        ClientCode = clientCode;
        ClientPaymentId = clientPaymentId;
    }

    public string ClientCode { get; }

    public string ClientPaymentId { get; }
}

public class PaymentProcessQueueQueryHandler : IRequestHandler<PaymentProcessQueueQuery, PaymentProcessQueue?>
{
    private readonly IDataConnection<SqlConnection> _connection;

    public PaymentProcessQueueQueryHandler(IDataConnection<SqlConnection> connection) =>
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));

    public Task<PaymentProcessQueue?> Handle(PaymentProcessQueueQuery request, CancellationToken cancellationToken) =>
        _connection.GetFirstOrDefaultAsync<PaymentProcessQueue?, SqlConnection>(
            "[dbo].[usp_PaymentProcessQueue_GetByClientCodeAndClientPaymentId]",
            new
            {
                request.ClientCode,
                request.ClientPaymentId
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
}
