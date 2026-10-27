using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Optum.PaymentRouting.Data.Abstractions;

namespace VPay.Optum.PaymentRouting.Data.Sql.InboundHeader.Queries
{
    public static class GetInboundHeaderByOptumBatchIdSql
    {
        public record Query : IRequest<Result>
        {
            public string OptumBatchId { get; set; }
        }

        public record Result
        {
            public long Id { get; init; }
            public long InboundBatchId { get; init; }
            public string HeaderData { get; init; }
            public string PayerSubpayerId { get; init; }
            public string PayerAlias { get; init; }
            public string OptumBatchId { get; init; }
            public string Status { get; init; }
            public DateTime LastModifiedDateTime { get; init; }
            public string LastModifiedBy { get; set; }
        }
        
        [UsedImplicitly]
        public class Handler : IRequestHandler<Query, Result>
        {
            private readonly IDataConnection<SqlConnection> _connection;

            public Handler(IDataConnection<SqlConnection> connection) => _connection = connection;

            public Task<Result> Handle(Query query, CancellationToken cancellationToken)
            {
                return _connection.GetFirstOrDefaultAsync<Result>(
                    "[dbo].[usp_InboundHeader_GetByOptumBatchId]", query, cancellationToken
               );
            }
        }
    }
}
