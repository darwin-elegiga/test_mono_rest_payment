using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Optum.PaymentRouting.Data.Abstractions;

namespace VPay.Optum.PaymentRouting.Data.Sql.BatchManifest
{
    public static class GetBatchManifestSql
    {
        public record Query(long InboundBatchId) : IRequest<BatchManifest>;

        [UsedImplicitly]
        public class Handler : IRequestHandler<Query, BatchManifest>
        {
            private readonly IDataConnection<SqlConnection> _connection;

            public Handler(IDataConnection<SqlConnection> connection) => _connection = connection;

            public async Task<BatchManifest> Handle(Query request, CancellationToken cancellationToken)
            {
                using (var multi = await _connection.GetMultipleAsync("dbo.[usp_InboundBatch_GetManifest]", request, cancellationToken))
                {
                    BatchManifest? bm = (await multi.ReadAsync<BatchManifest>()).FirstOrDefault();
                    if (bm != null)
                    {
                        bm.InboundPaymentIds = (await multi.ReadAsync<long>()).ToList();
                    }

                    return bm;
                }
            }
        }
    }
}
