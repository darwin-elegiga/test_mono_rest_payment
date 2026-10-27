using System.Data;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Gateway.Data.Connection;

namespace VPay.Gateway.Data.Sql.Payments;

public class PaymentProcessQueueFindNextCommand : IRequest<PaymentProcessQueue?>
{
    public PaymentProcessQueueFindNextCommand(string clientCode, string clientPaymentId)
    {
        ClientCode = clientCode;
        ClientPaymentId = clientPaymentId;
    }

    public string ClientCode { get; }

    public string ClientPaymentId { get; }
}

public class PaymentProcessQueueFindNextCommandHandler : IRequestHandler<PaymentProcessQueueFindNextCommand, PaymentProcessQueue?>
{
    private readonly IDataConnection<SqlConnection> _connection;

    public PaymentProcessQueueFindNextCommandHandler(IDataConnection<SqlConnection> connection) =>
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));

    public Task<PaymentProcessQueue?> Handle(PaymentProcessQueueFindNextCommand request, CancellationToken cancellationToken) =>
        _connection.SaveFirstOrDefaultAsync<PaymentProcessQueue?, SqlConnection>(
            "[dbo].[usp_PaymentProcessQueue_FindNextRequest]",
            new
            {
                request.ClientCode,
                request.ClientPaymentId
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
}
