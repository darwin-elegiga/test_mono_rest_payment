using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Gateway.Data.Connection;

namespace VPay.Gateway.Data.Sql.Payments;

public class PaymentVendorQueryHandler : IRequestHandler<PaymentVendorQuery, VpayTransaction>
{
    private readonly IDataConnection<SqlConnection> _connection;

    public PaymentVendorQueryHandler(IDataConnection<SqlConnection> connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    public async Task<VpayTransaction> Handle(PaymentVendorQuery request, CancellationToken cancellationToken)
    {
        var result = await _connection.GetMultipleResultAsync<VpayTransaction>(
            "[dbo].[usp_GetTransactionIdByPreTransId]",
            new { request.PreTransactionId },
            cancellationToken
        ).ConfigureAwait(false);

        var transaction = result.FirstOrDefault();

        if (transaction == null)
        {
            throw new InvalidOperationException("No transaction found for the given PreTransactionId.");
        }

        return transaction;
    }
}
