using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Optum.PaymentRouting.Data.Abstractions;


namespace VPay.Optum.PaymentRouting.Data.Sql.BatchSummary
{
    public static class GetBatchSummarySql
    {
        public record Query(long InboundBatchId) : IRequest<BatchSummary>;
        public class Handler : IRequestHandler<Query, BatchSummary>
        {
            private readonly IDataConnection<SqlConnection> _connection;

            public Handler(IDataConnection<SqlConnection> connection)
            {
                _connection = connection;
            }

            public Task<BatchSummary> Handle(Query request, CancellationToken cancellationToken)
            {
                return _connection.GetFirstOrDefaultAsync<BatchSummary>("dbo.usp_InboundBatch_Summary", request, cancellationToken);
            }
        }
    }
}
