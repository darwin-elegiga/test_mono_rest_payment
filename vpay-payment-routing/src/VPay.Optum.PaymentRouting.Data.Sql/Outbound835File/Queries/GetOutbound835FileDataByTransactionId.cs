using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Optum.PaymentRouting.Data.Abstractions;
using VPay.Optum.PaymentRouting.Data.Sql.Models;

namespace VPay.Optum.PaymentRouting.Data.Sql.Outbound835File.Queries
{
    public class GetOutbound835FileDataByTransactionId
    {
        public record Query(long TransactionId) : IRequest<Outbound835Data>;

        public class Handler : IRequestHandler<Query, Outbound835Data>
        {
            private readonly IDataConnection<SqlConnection> _connection;

            public Handler(IDataConnection<SqlConnection> connection) => _connection = connection;

            public async Task<Outbound835Data> Handle(Query request, CancellationToken cancellationToken)
            {
                return await _connection.GetFirstOrDefaultAsync<Outbound835Data>(
                    "[dbo].[usp_Outbound835Data_GetByTransactionId]",
                    request,
                    cancellationToken
                );
            }
        }
    }
}
