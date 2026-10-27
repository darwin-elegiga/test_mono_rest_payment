using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Optum.PaymentRouting.Data.Abstractions;

namespace VPay.Optum.PaymentRouting.Data.Sql.InboundPayment.Commands
{
    public static class UpdateInboundPaymentRefEoSql
    {
        public record Command : IRequest
        {
            public long InboundPaymentId { get; set; }
            public string OutboundRefEo { get; set; }
            public string ModifiedBy { get; set; }
        }

        [UsedImplicitly]
        public class Handler : IRequestHandler<Command>
        {
            private readonly IDataConnection<SqlConnection> _connection;

            public Handler(IDataConnection<SqlConnection> connection) => _connection = connection;

            public async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
            {
                await _connection.SaveExecuteAsync("[dbo].[usp_InboundPayment_Update_RefEo]", request, cancellationToken).ConfigureAwait(false);
                return Unit.Value;
            }
        }
    }
}
