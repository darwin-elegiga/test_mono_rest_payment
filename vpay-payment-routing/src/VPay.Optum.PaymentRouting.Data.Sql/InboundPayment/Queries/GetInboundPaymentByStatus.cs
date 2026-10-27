#nullable enable
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Optum.PaymentRouting.Data.Abstractions;
using VPay.Optum.PaymentRouting.Data.SmartEnums;

namespace VPay.Optum.PaymentRouting.Data.Sql.InboundPayment.Queries
{
    /// <summary>
    /// Lightweight Get Payments by Status.  Only get "clob" when it's needed within the context
    /// being used.
    /// </summary>
    public class GetInboundPaymentByStatus
    {
        public record Query(PaymentStatus Status) : IRequest<IEnumerable<Result>>;

        public record Result
        {
            public long InboundPaymentId { get; init; } = default!;
            public string PayerAlias { get; init; } = default!;
            public string OutboundRefEo { get; init; } = default!;
            public string UserKey { get; init; } = default!;
            public decimal PaymentAmount { get; init; } = default!;
            public PaymentStatus Status { get; init; } = default!;
            public string RemitMethod { get; set; }
        }

        public class Handler : IRequestHandler<Query, IEnumerable<Result>>
        {
            private readonly IDataConnection<SqlConnection> _connection;

            public Handler(IDataConnection<SqlConnection> connection) => _connection = connection;

            public async Task<IEnumerable<Result>> Handle(Query query, CancellationToken cancellationToken)
            {
                return await _connection.GetAsync<Result>("dbo.[usp_InboundPayment_Get_MetaData_ByStatus]", query, cancellationToken);
            }
        }
    }
}
