using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Optum.PaymentRouting.Data.Abstractions;

namespace VPay.Optum.PaymentRouting.Data.Sql.InboundPayment.Queries
{
    public static class GetInboundPaymentWithClobByUserKeyPayer835IdSql
    {
        public class Result
        {
            public long InboundBatchId { get; set; }
            public long InboundPaymentId { get; set; }
            public string OptumBatchId { get; set; }
            public string UserKey { get; set; }
            public string Payer835Id { get; set; }
            public string FileProcId { get; set; }
            public string PayerAlias { get; set; }
            public string ProvTIN { get; set; }
            public string PayerSubpayerId { get; set; }
            public string ZipCode { get; set; }
            public decimal PaymentAmount { get; set; }
            public DateTime BatchCreatedDateTime { get; set; }
            public DateTime PaymentCreatedDateTime { get; set; }
            public string PaymentClob { get; set; }
            public string RemitMethod { get; set; }
        }

        public record Query(string UserKey, string Payer835Id) : IRequest<Result>;

        public class Handler : IRequestHandler<Query, Result>
        {
            private readonly IDataConnection<SqlConnection> _connection;

            public Handler(IDataConnection<SqlConnection> connection)
            {
                _connection = connection;
            }

            public async Task<Result> Handle(Query request, CancellationToken cancellationToken)
            {
                return await _connection.GetFirstOrDefaultAsync<Result>("dbo.[usp_InboundPayment_GetBy_UserKey_Payer835Id]", request, cancellationToken);
            }
        }
    }
}
