using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Optum.PaymentRouting.Data.Abstractions;

namespace VPay.Optum.PaymentRouting.Data.Sql.OutboundFinSeg.Commands
{
    public class GetNextFinSegSequence
    {
        public record Command(string PayerAlias, string DateStamp, string CreatedBy) : IRequest<Models.OutboundFinSegSequence>;

        public class Handler : IRequestHandler<Command, Models.OutboundFinSegSequence>
        {
            private readonly IDataConnection<SqlConnection> _connection;

            public Handler(IDataConnection<SqlConnection> connection) => _connection = connection;

            public async Task<Models.OutboundFinSegSequence> Handle(Command request, CancellationToken cancellationToken)
            {
                return await _connection.GetFirstOrDefaultAsync<Models.OutboundFinSegSequence>(
                    "dbo.usp_OutboundFinSegSequence_GetNextId",
                    new
                    {
                        request.PayerAlias,
                        request.DateStamp,
                        request.CreatedBy
                    },
                    cancellationToken
                );
            }
        }
    }
}
