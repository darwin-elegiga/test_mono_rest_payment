using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Optum.PaymentRouting.Data.Abstractions;
using VPay.Optum.PaymentRouting.Data.SmartEnums;

namespace VPay.Optum.PaymentRouting.Data.Sql.InboundPayment.Queries
{
    public class GetInboundPaymentWithClobByStatus
    {
        public record Query(PaymentStatus Status) : IRequest<IEnumerable<Result>>;

        public class Result
        {
            public long InboundPaymentId { get; set; }

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

            public PaymentStatus Status { get; set; }

            public string OutboundRefEo { get; set; }

            public string RemitMethod { get; set; }
        }

        public class Handler : IRequestHandler<Query, IEnumerable<Result>>
        {
            private readonly IDataConnection<SqlConnection> _connection;

            public Handler(IDataConnection<SqlConnection> connection) => _connection = connection;

            public async Task<IEnumerable<Result>> Handle(Query query, CancellationToken cancellationToken)
            {
                return await _connection.GetAsync<Result>("dbo.[usp_InboundPayment_GetByStatus]", query, cancellationToken);
            }
        }
    }
}
