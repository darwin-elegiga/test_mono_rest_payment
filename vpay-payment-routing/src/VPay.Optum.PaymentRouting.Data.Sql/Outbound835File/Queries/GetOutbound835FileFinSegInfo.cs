using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Optum.PaymentRouting.Data.Abstractions;
using VPay.Optum.PaymentRouting.Data.SmartEnums;

namespace VPay.Optum.PaymentRouting.Data.Sql.Outbound835File.Queries
{
    public static class GetOutbound835FileFinSegInfo
    {
        public record Query(long Outbound835FileId) : IRequest<IEnumerable<Result>>;

        public class Result
        {
            public string PayerAlias { get; set; }
            public Outbound835FileStatus Status { get; set; }
            public DateTime CreateDateTime { get; set; }
            public decimal PaymentAmount { get; set; }
            public string PayerId { get; set; }
            public string DateStamp { get; set; }
            public long SequenceNumber { get; set; }
        }

        [UsedImplicitly]
        public class Handler : IRequestHandler<Query, IEnumerable<Result>>
        {
            private readonly IDataConnection<SqlConnection> _connection;

            public Handler(IDataConnection<SqlConnection> connection) => _connection = connection;

            public Task<IEnumerable<Result>> Handle(Query query, CancellationToken cancellationToken)
            {
                return _connection.GetAsync<Result, SqlConnection>(
                    "dbo.[usp_Outbound835File_GetFinSegInfo]",
                    query,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken
                );
            }
        }
    }
}
