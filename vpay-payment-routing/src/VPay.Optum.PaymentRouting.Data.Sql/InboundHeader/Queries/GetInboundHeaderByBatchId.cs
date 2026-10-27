using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Optum.PaymentRouting.Data.Abstractions;

namespace VPay.Optum.PaymentRouting.Data.Sql.InboundHeader.Queries
{
    public class GetInboundHeaderByBatchId
    {
        public class Query : IRequest<Models.InboundHeader>
        {
            public long InboundBatchId { get; set; }
        }

        public class Handler : IRequestHandler<Query, Models.InboundHeader>
        {
            private readonly IDataConnection<SqlConnection> _connection;

            public Handler(IDataConnection<SqlConnection> connection)
            {
                _connection = connection;
            }

            public async Task<Models.InboundHeader> Handle(Query request, CancellationToken cancellationToken)
            {
                return await _connection.GetFirstOrDefaultAsync<Models.InboundHeader>(
                    "dbo.usp_InboundHeader_GetByBatchId", request, cancellationToken
                ).ConfigureAwait(false);
            }
        }
    }
}
