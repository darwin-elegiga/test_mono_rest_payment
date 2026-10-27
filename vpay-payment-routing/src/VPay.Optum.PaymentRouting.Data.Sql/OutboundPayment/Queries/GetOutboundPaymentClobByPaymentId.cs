using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Optum.PaymentRouting.Data.Abstractions;

namespace VPay.Optum.PaymentRouting.Data.Sql.OutboundPayment.Queries
{
    public class GetOutboundPaymentClobByPaymentId
    {
        public record Query(long InboundPaymentId) : IRequest<Result>;

        public class Result
        {
            public long Id { get; set; }
            public long InboundPaymentId { get; set; }
            public string Clob { get; set; }
            public DateTime CreatedDateTime { get; set; }
        }

        public class Handler : IRequestHandler<Query, Result>
        {
            private readonly IDataConnection<SqlConnection> _connection;

            public Handler(IDataConnection<SqlConnection> connection) => _connection = connection;

            public Task<Result> Handle(Query query, CancellationToken cancellationToken)
            {
                return _connection.GetFirstOrDefaultAsync<Result>("dbo.[usp_OutboundPaymentClob_GetBy_InboundPaymentId]", query, cancellationToken);
            }
        }
    }
}
