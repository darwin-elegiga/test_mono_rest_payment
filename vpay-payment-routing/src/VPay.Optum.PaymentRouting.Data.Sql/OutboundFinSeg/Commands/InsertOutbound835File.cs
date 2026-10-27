using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Optum.PaymentRouting.Data.Abstractions;
using VPay.Optum.PaymentRouting.Data.Sql.Helpers;

namespace VPay.Optum.PaymentRouting.Data.Sql.OutboundFinSeg.Commands
{

    /// <summary>
    /// Inserts both the segments for the FINSEG TXT and the 835
    /// </summary>
    public class InsertOutboundFinSegFiles
    {
        public record Result(long OutboundFinSegTxtFileId, long OutboundFinSeg835FileId);

        public record Command(long OutboundFinSegSequenceId, string PayerAlias, string OutboundRefEo, string CreatedBy,
            IEnumerable<long> PaymentIds) : IRequest<Result>;

        public class Handler : IRequestHandler<Command, Result>
        {
            private readonly IDataConnection<SqlConnection> _connection;

            public Handler(IDataConnection<SqlConnection> connection) => _connection = connection;

            public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
            {
                return await _connection.GetFirstOrDefaultAsync<Result>(
                    "dbo.usp_Outbound835File_FinSeg_Insert",
                    new
                    {
                        request.OutboundFinSegSequenceId,
                        request.PayerAlias,
                        request.OutboundRefEo,
                        request.CreatedBy,
                        Payments = request.PaymentIds.ToList().ToIdDataTable().AsTableValuedParameter("IdTable")
                    },
                    cancellationToken
                );
            }
        }
    }
}
