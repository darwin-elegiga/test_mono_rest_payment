#nullable enable

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
    public static class GetOutbound835FileTransactionIdsByOutbound835FileId
    {
        public record Query(long Outbound835FileId) : IRequest<IEnumerable<Result>>;

        public record Result(
            long Outbound835FileId,
            long InboundPaymentId,
            long TransactionId,
            string PayerAlias,
            string OutboundRefEO,
            string FilePath,
            Outbound835FileStatus Outbound835FileStatus,
            DateTime? Outbound835FileSentDateTime,
            DateTime Outbound835FileCreatedDateTime,
            DateTime Outbound835FileLastModifiedDateTime);

        [UsedImplicitly]
        public class Handler : IRequestHandler<Query, IEnumerable<Result>>
        {
            private readonly IDataConnection<SqlConnection> _connection;

            public Handler(IDataConnection<SqlConnection> connection) => _connection = connection;

            public Task<IEnumerable<Result>> Handle(Query query, CancellationToken cancellationToken)
            {
                return _connection.GetAsync<Result, SqlConnection>(
                    "dbo.[usp_Outbound835FileTransactionIds_By_Outbound835FileId]",
                    query,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken
                );
            }
        }

    }
}
