using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Optum.PaymentRouting.Data.Abstractions;
using VPay.Optum.PaymentRouting.Data.Sql.Models;

namespace VPay.Optum.PaymentRouting.Data.Sql.InboundBatch.Queries
{
    public static class GetIncompleteBatchesSql
    {
        public record Query(int? MinutesLag) : IRequest<IEnumerable<IncompleteBatchDetails>>;
        

        public class Handler : IRequestHandler<Query, IEnumerable<IncompleteBatchDetails>>
        {
            private readonly IDataConnection<SqlConnection> _connection;

           public Handler(IDataConnection<SqlConnection> connection) => _connection = connection;
            public Task<IEnumerable<IncompleteBatchDetails>> Handle(Query request, CancellationToken cancellationToken)
            {
                return _connection.GetAsync<IncompleteBatchDetails, SqlConnection>(
                    "dbo.usp_InboundBatch_IncompleteBatchDetails",
                    request,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken
               );
            }
        }
    }
}
