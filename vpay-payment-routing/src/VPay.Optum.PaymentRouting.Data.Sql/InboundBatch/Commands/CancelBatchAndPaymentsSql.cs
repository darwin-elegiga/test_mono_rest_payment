using System.Data;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Optum.PaymentRouting.Data.Abstractions;

namespace VPay.Optum.PaymentRouting.Data.Sql.InboundBatch.Commands
{
    public class CancelBatchAndPaymentsSql
    {
        public record Command(long InboundBatchId, string JiraTicket) : IRequest;

        [UsedImplicitly]
        public class Handler : IRequestHandler<Command>
        {
            private readonly IDataConnection<SqlConnection> _connection;

            public Handler(IDataConnection<SqlConnection> connection) => _connection = connection;

            public async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
            {
                await _connection.SaveExecuteAsync(
                    "dbo.[usp_InboundBatch_CancelAllPayments]",
                    request,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken
                ).ConfigureAwait(false);
                return Unit.Value;
            }
        }
    }
}
