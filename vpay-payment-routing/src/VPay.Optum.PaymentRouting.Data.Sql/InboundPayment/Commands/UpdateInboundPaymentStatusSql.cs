using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Optum.PaymentRouting.Data.Abstractions;
using VPay.Optum.PaymentRouting.Data.SmartEnums;

namespace VPay.Optum.PaymentRouting.Data.Sql.InboundPayment.Commands
{
    public static class UpdateInboundPaymentStatusSql
    {
        public class Command : IRequest<Unit>
        {
            public long InboundPaymentId { get; set; }
            public PaymentStatus Status { get; set; }
            public string ModifiedBy { get; set; }
        }

        public class Handler : IRequestHandler<Command, Unit>
        {
            private readonly IDataConnection<SqlConnection> _connection;

            public Handler(IDataConnection<SqlConnection> connection) => _connection = connection;

            public async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
            {
                await _connection.SaveExecuteAsync("dbo.[usp_InboundPayment_Update_Status]", request, cancellationToken).ConfigureAwait(false);
                return Unit.Value;
            }
        }
    }
}
