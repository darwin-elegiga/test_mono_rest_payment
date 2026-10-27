using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Optum.PaymentRouting.Data.Abstractions;
using VPay.Optum.PaymentRouting.Data.Sql.Models;

namespace VPay.Optum.PaymentRouting.Data.Sql.ApReap.Queries
{
    public static class GetApMessageTrackingByIdSql
    {
        public record Query : IRequest<ApMessageTracking>
        {
            public long Id { get; init; }
        }

        [UsedImplicitly]
        public class Handler : IRequestHandler<Query, ApMessageTracking>
        {
            private readonly IDataConnection<SqlConnection> _connection;

            public Handler(IDataConnection<SqlConnection> connection) => _connection = connection;

            public async Task<ApMessageTracking> Handle(Query query, CancellationToken cancellationToken)
            {
                return await _connection.GetFirstOrDefaultAsync<ApMessageTracking>(
                    "[dbo].[usp_ApMessageTracking_GetById]", query, cancellationToken
                );
            }
        }
    }
}
