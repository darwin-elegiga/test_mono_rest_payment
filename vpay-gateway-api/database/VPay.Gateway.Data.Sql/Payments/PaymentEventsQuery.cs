using System.Data;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Gateway.Data.Connection;

namespace VPay.Gateway.Data.Sql.Payments;

public class PaymentEventsQuery : IRequest<IReadOnlyCollection<PaymentEvent>>
{
    public PaymentEventsQuery(string clientCode, string clientPaymentId)
    {
        ClientCode = clientCode;
        ClientPaymentId = clientPaymentId;
    }

    public string ClientCode { get; }

    public string ClientPaymentId { get; }
}

public class PaymentEventsQueryHandler : IRequestHandler<PaymentEventsQuery, IReadOnlyCollection<PaymentEvent>>
{
    private readonly IDataConnection<SqlConnection> _connection;

    public PaymentEventsQueryHandler(IDataConnection<SqlConnection> connection) =>
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));

    public async Task<IReadOnlyCollection<PaymentEvent>> Handle(PaymentEventsQuery request, CancellationToken cancellationToken)
    {
        var result = await _connection.GetAsync<PaymentEvent, SqlConnection>(
            "[dbo].[usp_PaymentEvent_GetByClientCodeAndClientPaymentId]",
            new
            {
                request.ClientCode,
                request.ClientPaymentId
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return result.ToList().AsReadOnly();
    }
}
