using System.Data;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Optum.PaymentRouting.Data.Abstractions;

namespace VPay.Optum.PaymentRouting.Data.Sql.InboundBatch.Commands
{
    /// <exception cref="SqlException">when [InboundBatch] record not found for <see cref="Command.InboundBatchId"/></exception>
    /// <exception cref="SqlException">when [InboundBatch].[Status] is COMPLETE for [InboundBatch].[Id] = [InboundPayment].[InboundBatchId]</exception>
    public class UpdateInboundBatchTotalsSql
    {
        public record Command(
            long InboundBatchId,
            int TotalPaymentCount,
            decimal TotalPaymentAmount) : IRequest;

        [UsedImplicitly]
        public class Handler : IRequestHandler<Command>
        {
            private readonly IDataConnection<SqlConnection> _connection;

            public Handler(IDataConnection<SqlConnection> connection) => _connection = connection;

            public async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
            {
                await _connection.SaveExecuteAsync(
                    "dbo.[usp_InboundBatch_Update_Totals]",
                    request,
                    cancellationToken
                ).ConfigureAwait(false);

                return Unit.Value;
            }
        }
    }
}
