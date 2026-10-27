using System.Data;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Gateway.Data.Connection;

namespace VPay.Gateway.Data.Sql.Payments;

public class PaymentProcessQueueDeleteCommand : IRequest<bool>
{
    public PaymentProcessQueueDeleteCommand(string clientCode, string clientPaymentId)
    {
        ClientCode = clientCode;
        ClientPaymentId = clientPaymentId;
    }

    public string ClientCode { get; }

    public string ClientPaymentId { get; }
}

public class PaymentProcessQueueDeleteCommandHandler : IRequestHandler<PaymentProcessQueueDeleteCommand, bool>
{
    private readonly IDataConnection<SqlConnection> _connection;

    public PaymentProcessQueueDeleteCommandHandler(IDataConnection<SqlConnection> connection) =>
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));

    public async Task<bool> Handle(PaymentProcessQueueDeleteCommand request, CancellationToken cancellationToken)
    {
        var value = await _connection.SaveExecuteScalarAsync<int, SqlConnection>(
            "[dbo].[usp_PaymentProcessQueue_Delete]",
            new
            {
                request.ClientCode,
                request.ClientPaymentId
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        return value > 0;
    }
}
