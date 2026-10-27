using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Optum.PaymentRouting.Data.Abstractions;

namespace VPay.Optum.PaymentRouting.Data.Sql.Outbound835File.Queries
{
    public class GetOutbound835FileById
    {
        public record Query(long Outbound835FileId) : IRequest<Models.Outbound835File>;

        public class Handler : IRequestHandler<Query, Models.Outbound835File>
        {
            private readonly IDataConnection<SqlConnection> _connection;

            public Handler(IDataConnection<SqlConnection> connection) => _connection = connection;

            public async Task<Models.Outbound835File> Handle(Query request, CancellationToken cancellationToken)
            {
                using (var multi = await _connection.GetMultipleAsync("dbo.[usp_Outbound835File_GetById]", request, cancellationToken))
                {
                    Models.Outbound835File file = (await multi.ReadFirstOrDefaultAsync<Models.Outbound835File>());
                    if (file != null)
                    {
                        file.PaymentIds = (await multi.ReadAsync<long>()).ToList();
                    }

                    return file;
                }
            }
        }
    }
}
