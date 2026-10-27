using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Optum.PaymentRouting.Data.Abstractions;

namespace VPay.Optum.PaymentRouting.Data.Sql.BatchSummary
{
    public static class InsertInboundPaymentSql
    {
        public record Command(
            string OptumBatchId,
            string Payer835Id,
            string PayerSubpayerId,
            string FileProcId,
            string PayerAlias,
            string ProvTIN,
            string ZipCode,
            string UserKey,
            decimal PaymentAmount,
            string PaymentClob,
            string RemitMethod = "DATA") : IRequest<BatchSummary>;
        public class Handler : IRequestHandler<Command, BatchSummary>
        {
            private readonly IDataConnection<SqlConnection> _connection;

            public Handler(IDataConnection<SqlConnection> connection)
            {
                _connection = connection;
            }

            public async Task<BatchSummary> Handle(Command request, CancellationToken cancellationToken)
            {
                var inboundBatchId = await _connection.SaveExecuteScalarAsync<long>(
                    "dbo.usp_InboundPayment_Insert", request, cancellationToken
               ).ConfigureAwait(false);

                return await _connection.GetFirstOrDefaultAsync<BatchSummary>(
                    "dbo.[usp_InboundBatch_Summary]", new { InboundBatchId = inboundBatchId }, cancellationToken
                 ).ConfigureAwait(false);
            }
        }
    }
}
