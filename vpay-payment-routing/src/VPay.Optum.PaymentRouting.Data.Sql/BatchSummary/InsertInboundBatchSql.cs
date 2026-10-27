using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Optum.PaymentRouting.Data.Abstractions;

namespace VPay.Optum.PaymentRouting.Data.Sql.BatchSummary
{
    public static class InsertInboundBatchSql
    {
        public record Query(
            string OptumBatchId,
            string Payer835Id,
            int TotalPaymentCount,
            decimal TotalPaymentAmount) : IRequest<BatchSummary>;
        public class Handler : IRequestHandler<Query, BatchSummary>
        {
            private readonly IDataConnection<SqlConnection> _connection;

            public Handler(IDataConnection<SqlConnection> connection)
            {
                _connection = connection;
            }

            public async Task<BatchSummary> Handle(Query request, CancellationToken cancellationToken)
            {
                var inboundBatchId = await _connection.SaveExecuteScalarAsync<long>("dbo.usp_InboundBatch_Insert", request, cancellationToken).ConfigureAwait(false);

                return await _connection.GetFirstOrDefaultAsync<BatchSummary>("dbo.[usp_InboundBatch_Summary]", new { InboundBatchId = inboundBatchId }, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
