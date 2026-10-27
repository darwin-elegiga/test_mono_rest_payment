using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Optum.PaymentRouting.Data.Abstractions;

namespace VPay.Optum.PaymentRouting.Data.Sql.InboundHeader
{
    public static class GetInboundHeaderByIdSql
    {
        public record Query(long InboundHeaderId) : IRequest<InboundHeader>;
        public class Handler : IRequestHandler<Query, InboundHeader>
        {
            private readonly IDataConnection<SqlConnection> _connection;

            public Handler(IDataConnection<SqlConnection> connection)
            {
                _connection = connection;
            }

            public Task<InboundHeader> Handle(Query request, CancellationToken cancellationToken)
            {
                return _connection.GetFirstOrDefaultAsync<InboundHeader>(
                    "dbo.usp_InboundHeader_GetById", request, cancellationToken
               );
            }
        }
    }
}
