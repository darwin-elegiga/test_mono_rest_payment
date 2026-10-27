using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Optum.PaymentRouting.Data.Abstractions;

namespace VPay.Optum.PaymentRouting.Data.Sql.InboundBatch.Queries
{
    public static class GetInboundBatchByOptumBatchIdSql
    {
        public record Query(string OptumBatchId) : IRequest<Result>;

        public class Result
        {
            public long Id { get; set; }
            public string OptumBatchId { get; set; }
            public int? TotalPaymentCount { get; set; }
            public decimal? TotalPaymentAmount { get; set; }
            public BatchStatus Status { get; set; }
        }

        public class Handler : IRequestHandler<Query, Result>
        {
            private readonly IDataConnection<SqlConnection> _connection;

            public Handler(IDataConnection<SqlConnection> connection)
            {
                _connection = connection;
            }

            public Task<Result> Handle(Query request, CancellationToken cancellationToken)
            {
                return _connection.GetFirstOrDefaultAsync<Result>(
                    "dbo.usp_InboundBatch_GetByOptumBatchId", request, cancellationToken);
            }
        }
    }
}
