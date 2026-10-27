using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Optum.PaymentRouting.Data.Abstractions;

namespace VPay.Optum.PaymentRouting.Data.Sql.OutboundFinSeg.Queries
{
    public class GetOutboundFinSegSequenceById
    {
        public record Query(long OutboundFinSegSequenceId) : IRequest<Models.OutboundFinSegSequence>;
        public class Handler : IRequestHandler<Query, Models.OutboundFinSegSequence>
        {
            private readonly IDataConnection<SqlConnection> _connection;

            public Handler(IDataConnection<SqlConnection> connection) => _connection = connection;

            public async Task<Models.OutboundFinSegSequence> Handle(Query request, CancellationToken cancellationToken)
            {
                return await _connection.GetFirstOrDefaultAsync<Models.OutboundFinSegSequence>(
                    "dbo.usp_OutboundFinSegSequence_GetById",
                    new
                    {
                        request.OutboundFinSegSequenceId
                    },
                    cancellationToken
                );
            }
        }
    }
}
