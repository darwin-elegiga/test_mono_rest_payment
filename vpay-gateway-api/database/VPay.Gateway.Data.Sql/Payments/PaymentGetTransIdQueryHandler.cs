using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Gateway.Data.Connection;

namespace VPay.Gateway.Data.Sql.Payments;
public class PaymentGetTransIdQueryHandler : IRequestHandler<PaymentTransactionQuery, VendorTransaction>
{
    private readonly IDataConnection<SqlConnection> _connection;

    public PaymentGetTransIdQueryHandler(IDataConnection<SqlConnection> connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    public async Task<VendorTransaction> Handle(PaymentTransactionQuery request, CancellationToken cancellationToken)
    {
        var result = await _connection.GetMultipleResultAsync<VendorTransaction>(
            "[dbo].[usp_GetPreTransIdByTransactionId]",
            new { request.TransactionId },
            cancellationToken
        ).ConfigureAwait(false);

        var pretransaction = result.FirstOrDefault();

        return pretransaction!;
    }
}
