using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Optum.PaymentRouting.Data.Abstractions;

namespace VPay.Optum.PaymentRouting.Data.Sql.InboundPayment.Queries
{
    public static class GetInboundPaymentWithClobByIdSql
    {
        public record Query : IRequest<Result>
        {
            public long InboundPaymentId { get; set; }
        }

        public class Result
        {
            public long Id { get; set; }

            public long InboundBatchId { get; set; }

            public string PaymentClob { get; set; }

            public string UserKey { get; set; }

            public string Payer835Id { get; set; }

            public string FileProcId { get; set; }

            public string PayerAlias { get; set; }

            public string ProvTIN { get; set; }

            public string PayerSubpayerId { get; set; }

            public string ZipCode { get; set; }

            public decimal PaymentAmount { get; set; }

            public DateTime CreateDateTime { get; set; }

            public string RemitMethod { get; set; }
        }

        [UsedImplicitly]
        public class Handler : IRequestHandler<Query, Result>
        {
            private readonly IDataConnection<SqlConnection> _connection;

            public Handler(IDataConnection<SqlConnection> connection) => _connection = connection;

            public Task<Result> Handle(Query query, CancellationToken cancellationToken)
            {
                return _connection.GetFirstOrDefaultAsync<Result>(
                    "dbo.[usp_InboundPayment_GetById]", query, cancellationToken
               );
            }
        }
    }
}
