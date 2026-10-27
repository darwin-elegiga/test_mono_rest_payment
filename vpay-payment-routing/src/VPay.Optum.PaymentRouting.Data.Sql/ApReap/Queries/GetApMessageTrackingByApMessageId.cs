using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Optum.PaymentRouting.Data.Abstractions;
using VPay.Optum.PaymentRouting.Data.Sql.Models;

namespace VPay.Optum.PaymentRouting.Data.Sql.ApReap.Queries
{
    public class GetApMessageTrackingByApMessageId
    {
        public class Query : IRequest<ApMessageTracking>
        {
            public long ApMessageId { get; set; }
        }

        public class Handler : IRequestHandler<Query, ApMessageTracking>
        {
            private readonly IDataConnection<SqlConnection> _connection;

            public Handler(IDataConnection<SqlConnection> connection)
            {
                _connection = connection;
            }

            public async Task<ApMessageTracking> Handle(Query request, CancellationToken cancellationToken)
            {
                return await _connection.GetFirstOrDefaultAsync<ApMessageTracking>(
                    "[dbo].[usp_ApMessageTracking_GetByApMessageId]", request, cancellationToken
                );
            }
        }
    }
}
