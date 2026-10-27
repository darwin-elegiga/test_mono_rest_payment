using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Optum.PaymentRouting.Data.Abstractions;

namespace VPay.Optum.PaymentRouting.Data.Sql.OutboundPayment.Commands
{
    public static class InsertOutboundPaymentClobSql
    {
        public record Command : IRequest
        {
            public long InboundPaymentId { get; set; }
            public string Clob { get; set; }
        }

        [UsedImplicitly]
        public class Handler : IRequestHandler<Command>
        {
            private readonly IDataConnection<SqlConnection> _connection;

            public Handler(IDataConnection<SqlConnection> connection) => _connection = connection;

            public async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
            {
                await _connection.SaveExecuteAsync(
                    "[dbo].[usp_OutboundPaymentClob_Insert]",
                    request,
                    cancellationToken: cancellationToken
                ).ConfigureAwait(false);
                return Unit.Value;
            }
        }
    }
}
