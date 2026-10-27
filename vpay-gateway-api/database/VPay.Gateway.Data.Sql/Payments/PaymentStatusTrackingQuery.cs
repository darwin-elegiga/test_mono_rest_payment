using System.Data;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Gateway.Data.Connection;

namespace VPay.Gateway.Data.Sql.Payments;

public class PaymentStatusTrackingQuery : IRequest<IReadOnlyCollection<PaymentStatusTracking>>
{
    public PaymentStatusTrackingQuery(long paymentId)
    {
        PaymentId = paymentId;
    }

    public long PaymentId { get; }
}

public class PaymentStatusTrackingQueryHandler : IRequestHandler<PaymentStatusTrackingQuery, IReadOnlyCollection<PaymentStatusTracking>>
{
    private readonly IDataConnection<SqlConnection> _connection;

    public PaymentStatusTrackingQueryHandler(IDataConnection<SqlConnection> connection) =>
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));

    public async Task<IReadOnlyCollection<PaymentStatusTracking>> Handle(PaymentStatusTrackingQuery request, CancellationToken cancellationToken)
    {
        var result = await _connection.GetAsync<PaymentStatusTracking, SqlConnection>(
            "[dbo].[usp_PaymentStatusTracking_GetByPaymentId]",
            new
            {
                request.PaymentId
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return result.ToList().AsReadOnly();
    }
}
